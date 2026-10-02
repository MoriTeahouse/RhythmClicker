// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 MoriTeahouse (森之宿茶室)
using ClickerGame;
using ClickerGame.Core;
int checks=0;
void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
void Reject<T>(Action action)where T:Exception{try{action();}catch(T){checks++;return;}throw new Exception("Expected "+typeof(T).Name);}
var map=new Beatmap{Bpm=120,Notes=new(){new(){Time=1,Column=0},new(){Time=2,Column=1},new(){Time=3,Column=2},new(){Time=3,Column=3}}};
var run=new PlayRun(map);Check(run.Indexed&&run.JudgedAccuracy==100,"Indexed and initial live accuracy");Check(run.HitAt(1,0)!=null&&run.JudgedAccuracy==100,"Future notes do not reduce accuracy");
Check(run.HitAt(2.08,1)!=null,"Great timing");run.MissesAt(3.3);Check(run.Complete&&run.Hit==2&&run.Miss==2&&run.Score==175,"Final miss accounting");Check(run.Accuracy==43.75&&run.JudgedAccuracy==43.75,"Weighted accuracy");
Check(run.Finish().Count==0,"Finish idempotent");run=new(map);run.HitAt(1,0);run.Finish();Check(run.Miss==3&&run.Accuracy==25,"Early failure");
var chord=new Beatmap{Bpm=120,Notes=Enumerable.Range(0,4).Select(i=>new Note{Time=1,Column=i}).ToList()};run=new(chord);foreach(int lane in new[]{2,0,3,1})Check(run.HitAt(1,lane)!=null,"Chord");
Check(run.Score==400&&run.Hit==4&&run.Miss==0&&run.HitAt(1,1)==null,"No double hit");
var legacy=Beatmap.LoadFromString("{\"Notes\":[{\"Time\":1,\"Column\":0}]}");legacy.NormalizeLegacyMetadata();legacy.Validate();Check(legacy.Bpm==120,"Legacy missing BPM");
Reject<InvalidDataException>(()=>new PlayRun(new(){Bpm=float.NaN}));Reject<InvalidDataException>(()=>new PlayRun(new(){Bpm=120,Notes=new(){new(){Time=float.PositiveInfinity}}}));Reject<InvalidDataException>(()=>new PlayRun(new(){Bpm=120,Notes=new(){new(){Time=1,Column=4}}}));
for(int song=0;song<3;song++)
{
    var easy=DemoLibrary.Chart(song,"easy");var hard=DemoLibrary.Chart(song,"hard");var advanced=DemoLibrary.Chart(song,"difficulty");easy.Validate();hard.Validate();advanced.Validate();
    Check(easy.Notes.Count<hard.Notes.Count&&hard.Notes.Count<advanced.Notes.Count,"Difficulty progression");Check(ReplayManager.ChartHash(hard)==ReplayManager.ChartHash(DemoLibrary.Chart(song,"hard")),"Stable chart");
    run=new(advanced);foreach(var note in advanced.Notes)Check(run.HitAt(note.Time,note.Column)!=null,"Demo note");Check(run.Complete&&run.Accuracy==100,"Demo result");
}
var settings=new GameSettings{MasterVolume=float.NaN,MusicVolume=2,SfxVolume=-1,OffsetMs=9999,VisualOffsetMs=-9999,ApproachSeconds=float.NaN,Lane0Key="F",Lane1Key="F"};settings.Normalize();
Check(settings.MasterVolume==0.8f&&settings.MusicVolume==1&&settings.SfxVolume==0&&settings.OffsetMs==500&&settings.VisualOffsetMs==-500&&settings.ApproachSeconds==1.6f,"Settings bounds");Check(settings.Lane0Key=="D"&&settings.Lane1Key=="F","Duplicate repaired");
settings.Lane0Key="123456";settings.Normalize();Check(settings.Lane0Key=="D","Undefined enum");settings.Lane0Key="Enter";settings.Normalize();Check(settings.Lane0Key=="D","Reserved binding");
string root=Path.Combine(Path.GetTempPath(),"MoriTeahouse-Rhythm-Tests",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
string file=Path.Combine(root,"settings.rc");var store=new SettingsManager(file);store.Settings.MasterVolume=0.4f;store.Save();store.Settings.MasterVolume=0.6f;store.Save();
Check(new SettingsManager(file).Settings.MasterVolume==0.6f,"Settings roundtrip");Check(RcFileManager.ReadEncrypted<GameSettings>(file+".bak").MasterVolume==0.4f,"Backup settings");
File.WriteAllText(file,"broken");Check(new SettingsManager(file).Settings.MasterVolume==0.4f,"Restore corrupt settings");
var recoveredSettings=new SettingsManager(file);recoveredSettings.Save();
Check(RcFileManager.ReadEncrypted<GameSettings>(file+".bak").MasterVolume==0.4f&&Directory.GetFiles(root,"settings.rc.corrupt-*").Length==1,"Recovery preserves good backup and bad original");
byte[] futureSettings=File.ReadAllBytes(file);futureSettings[4]=2;File.WriteAllBytes(file,futureSettings);
Reject<UnsupportedRcVersionException>(()=>recoveredSettings.Save());
Check(File.ReadAllBytes(file).SequenceEqual(futureSettings),"Future RC format never overwritten");
string chartPath=Path.Combine(root,"chart.rcm");RcFileManager.WriteBeatmap(chartPath,map);RcFileManager.WriteBeatmap(chartPath,chord);Check(RcFileManager.ReadBeatmap(chartPath+".bak").Notes[1].Time==2,"Backup chart magic");
string legacyJson=Path.Combine(root,"legacy.json");File.WriteAllText(legacyJson,"{\"Bpm\":120,\"Notes\":[]}");byte[] preserved=File.ReadAllBytes(chartPath);
Check(RcFileManager.MigrateJsonToRcm(legacyJson,chartPath),"Existing migration");Check(preserved.SequenceEqual(File.ReadAllBytes(chartPath)),"Never overwrite edited chart");
var records=new ReplayManager(Path.Combine(root,"Replays"));records.StartRecording(map);records.RecordJudgement(1,0,"PERFECT",100,1,1,0);
var replay=records.StopRecording("test/song","easy","guest",100,1,1,0,100,"SS");replay.Validate();Check(replay.Events[0].NoteTime==1&&replay.BeatmapHash==ReplayManager.ChartHash(map),"Replay identity");
records.StartRecording();records.RecordEvent(1,0,"PERFECT",100,1);var old=records.StopRecording("test/song","easy","guest",100,1,1,0,100,"SS");Check(old.BeatmapHash==""&&old.Events[0].NoteTime==null,"Legacy hash reset");
Check(records.GetAllReplays().Count==2&&records.GetBestReplay("test/song","easy")!=null,"Distinct safe filenames");File.WriteAllText(Path.Combine(root,"Replays","corrupt.rcp"),"broken");Check(records.GetAllReplays().Count==2,"Corrupt replay skipped");
Reject<InvalidDataException>(()=>new ReplayData{SongId="test",Accuracy=double.NaN}.Validate());Reject<InvalidDataException>(()=>new ReplayData{SongId="test",BeatmapHash=null!}.Validate());
Reject<InvalidDataException>(()=>new ReplayData{SongId="test",Events=new(){new(){Time=2,Column=0,Judgment="MISS"},new(){Time=1,Column=0,Judgment="MISS"}}}.Validate());
AppPaths.InstallRoot=root;AppPaths.EnsureDirectories();Check(AppPaths.SettingsFilePath.StartsWith(root)&&AppPaths.ReplaysPath.StartsWith(root)&&AppPaths.StatsDbPath.StartsWith(root)&&AppPaths.AccountsPath.StartsWith(root),"Data follows selected root");

string MakeZip(string name, params (string Name,string Text)[] entries)
{
    string path=Path.Combine(root,name+".zip");
    using var zip=System.IO.Compression.ZipFile.Open(path,System.IO.Compression.ZipArchiveMode.Create);
    foreach(var entry in entries){using var text=new StreamWriter(zip.CreateEntry(entry.Name).Open());text.Write(entry.Text);}
    return path;
}
string Digest(string path){using var input=File.OpenRead(path);return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(input));}
string safe=MakeZip("safe",("ClickerGame.exe","fixture"),("Assets/songs.json","[]"));
string staged=ClickerGame.Systems.VerifiedUpdatePackage.Stage(safe,Path.Combine(root,"safe-stage"),Digest(safe));
Check(File.Exists(staged)&&File.ReadAllText(Path.Combine(root,"safe-stage","Assets","songs.json"))=="[]","Verified portable release staged");
Reject<InvalidDataException>(()=>ClickerGame.Systems.VerifiedUpdatePackage.Stage(safe,Path.Combine(root,"bad-hash"),new string('0',64)));
Check(!Directory.Exists(Path.Combine(root,"bad-hash")),"Checksum checked before extraction");
foreach(var path in new[]{"../escape","C:/escape","/escape","Assets/../escape"})
{
    string badZip=MakeZip("path-"+checks,("ClickerGame.exe","fixture"),(path,"bad"));
    Reject<InvalidDataException>(()=>ClickerGame.Systems.VerifiedUpdatePackage.Stage(badZip,Path.Combine(root,"reject-"+checks),Digest(badZip)));
}
string duplicate=MakeZip("duplicate",("ClickerGame.exe","fixture"),("ASSETS/a.txt","a"),("assets/A.txt","b"));
Reject<InvalidDataException>(()=>ClickerGame.Systems.VerifiedUpdatePackage.Stage(duplicate,Path.Combine(root,"duplicate-stage"),Digest(duplicate)));
string noExe=MakeZip("noexe",("a.txt","a"));Reject<InvalidDataException>(()=>ClickerGame.Systems.VerifiedUpdatePackage.Stage(noExe,Path.Combine(root,"missing-stage"),Digest(noExe)));
Check(ClickerGame.Systems.UpdateManager.IsTrustedReleaseUrl("https://github.com/MoriTeahouse/RhythmClicker/releases/download/v0.6/game.zip"),"Owned release URL accepted");
Check(!ClickerGame.Systems.UpdateManager.IsTrustedReleaseUrl("http://github.com/MoriTeahouse/RhythmClicker/releases/download/v0.6/game.zip")&&!ClickerGame.Systems.UpdateManager.IsTrustedReleaseUrl("https://github.com/other/project/releases/download/v0.6/game.zip"),"Non-HTTPS and unrelated source rejected");

string osuText = """
osu file format v14
[General]
Mode:3
[Metadata]
Title:Original import fixture
Artist:MoriTeahouse
[Difficulty]
CircleSize:4
[TimingPoints]
0,500,4,2,1,50,1,0
[HitObjects]
64,192,1000,1,0,0:0:0:0:
192,192,1200,1,0,0:0:0:0:
320,192,1400,1,0,0:0:0:0:
448,192,1600,1,0,0:0:0:0:
""";
string osz=MakeZip("import",("nested/test.osu",osuText));
string importRoot=Path.Combine(root,"Imports");
var imported=OsuImporter.ImportOsz(osz,importRoot,"fixture");
Check(imported.Count==1&&imported[0].beatmap.Notes.Count==4&&imported[0].beatmap.Bpm==120,"Nested mania import");
Check(imported[0].beatmap.Notes.Select(n=>n.Column).SequenceEqual(new[]{0,1,2,3}),"Mania lane conversion");
Check(!Directory.GetDirectories(importRoot,".import-*").Any(),"Unique staging cleaned");
string escapeMedia=MakeZip("media-escape",("test.osu",osuText.Replace("Mode:3","Mode:3\nAudioFilename:../../private.wav")));
Reject<InvalidDataException>(()=>OsuImporter.ImportOsz(escapeMedia,importRoot));
string escapeZip=MakeZip("zip-escape",("../escape.txt","bad"));
Reject<InvalidDataException>(()=>OsuImporter.ImportOsz(escapeZip,importRoot));
Check(!Directory.GetDirectories(importRoot,".import-*").Any(),"Rejected import staging cleaned");
string invalidOsu=Path.Combine(root,"invalid.osu");File.WriteAllText(invalidOsu,osuText.Replace("1000,1,0","NaN,1,0"));
Reject<InvalidDataException>(()=>OsuImporter.Import(invalidOsu));
Console.WriteLine($"PASS: {checks} RhythmClicker checks; results, chords, legacy charts, settings, backups, replay identity. Fixtures: {root}");
