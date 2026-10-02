// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 MoriTeahouse (森之宿茶室)
using Microsoft.Xna.Framework;
using MatrixTea.Engine.Core.Audio;
namespace ClickerGame;
public partial class Game1
{
    private int smokeFrame,smokeStage,resultCaptureFrame;
    private bool smokePaused,smokeReplayPaused,playCapture,smokeVideoFrame;
    private bool IsSmoke=>Array.IndexOf(runArguments,"--smoke")>=0;
    private string? SmokeVideo{get{int i=Array.IndexOf(runArguments,"--smoke-video");return i>=0&&i+1<runArguments.Length?runArguments[i+1]:null;}}
    private string CaptureDirectory{get{int i=Array.IndexOf(runArguments,"--capture");return i>=0&&i+1<runArguments.Length?runArguments[i+1]:Path.Combine(Core.AppPaths.InstallRoot,"captures");}}
    private void UpdateSmoke(GameTime gameTime)
    {
        if(!IsSmoke)return;smokeFrame++;
        if(smokeStage==0&&smokeFrame>=8)
        {
            const string id="smoke_probe";
            string audio=Path.Combine(Core.AppPaths.AssetsPath,id+".wav");
            ProceduralScore.WriteWave(audio,2.1,120,57,0);
            var chart=new Beatmap{Bpm=120,AudioFile=id+".wav",VideoFile=SmokeVideo??"",Notes=new(){new(){Time=0.3f,Column=0},new(){Time=0.6f,Column=1},new(){Time=0.6f,Column=2},new(){Time=0.9f,Column=3}}};
            RcFileManager.WriteBeatmap(Core.AppPaths.BeatmapPath(id,"easy"),chart);
            songs.Add(new(){Id=id,Title="Integration probe",File=id+".wav",Difficulties=new(){"easy"}});
            currentSongIndex=songs.Count-1;currentDifficulty="easy";StartPlaying(false);
            if(state!=GameState.Playing)throw new Exception("Smoke could not start: "+syncStatusText);
            smokeStage=1;
        }
        if(smokeStage==1&&state==GameState.Playing&&SongTime>=0.35&&!smokePaused){ProbePause();smokePaused=true;}
        if(videoPlayer?.CurrentFrame!=null)smokeVideoFrame=true;
        if(smokeStage==1&&state==GameState.Result)smokeStage=2;
        if(smokeStage==3&&smokeFrame>resultCaptureFrame+12)
        {
            if(hitCount!=4||missCount!=0||score!=400||playRun?.Indexed!=true)throw new Exception("Smoke result mismatch.");
            var replay=replayManager!.GetBestReplay("smoke_probe","easy")??throw new Exception("Missing replay");
            StartReplayView(replay);if(state!=GameState.ReplayView)throw new Exception("Replay load failed: "+syncStatusText);smokeStage=4;
        }
        if(smokeStage==4&&state==GameState.ReplayView&&SongTime>=0.35&&!smokeReplayPaused){ProbePause();smokeReplayPaused=true;}
        if(smokeStage==4&&state==GameState.Result)
        {
            if(!isReplayRun||hitCount!=4||missCount!=0||score!=400||RoundAccuracy!=100||replayEventIndex!=4)throw new Exception("Replay result mismatch.");
            if(SmokeVideo!=null&&!smokeVideoFrame)throw new Exception("Video never uploaded a GPU frame. "+videoPlayer?.Error);
            smokeStage=5;
        }
        if(smokeStage==6)
        {
            File.WriteAllText(Path.Combine(Core.AppPaths.InstallRoot,"smoke-result.json"),System.Text.Json.JsonSerializer.Serialize(new{passed=true,score,hitCount,missCount,replay=true,pause=true,indexed=playRun!.Indexed,device=audioPlayer.IsAvailable,video=smokeVideoFrame}));
            Console.WriteLine($"RHYTHM_SMOKE_OK score={score} hit={hitCount} miss={missCount} replay=True pause=True indexed={playRun!.Indexed} device={audioPlayer.IsAvailable} video={smokeVideoFrame}");Exit();
        }
        if(HostNow>35)throw new TimeoutException($"Smoke did not complete: stage={smokeStage} time={SongTime}.");
    }
    private void ProbePause()
    {
        PauseRound();double frozen=SongTime;System.Threading.Thread.Sleep(35);
        if(Math.Abs(SongTime-frozen)>0.002||songTimeline.IsPlaying)throw new Exception("Paused timeline advanced.");
        ResumeRound();
    }
    private void DriveSmokeHits(double time)
    {
        if(playRun==null)return;
        foreach(var note in playRun.RemainingNotes.ToArray())
            if(time>=note.Time&&time-note.Time<0.1){var result=playRun.HitAt(note.Time,note.Column);if(result!=null)ApplyJudgement(result,time);}
    }
    private void CaptureSmoke()
    {
        if(!IsSmoke)return;
        string? name=smokeFrame==6?"menu.png":smokeStage==1&&state==GameState.Playing&&SongTime>=0.55&&!playCapture?"play.png":smokeStage==2?"result.png":smokeStage==5?"replay-result.png":null;
        if(name==null)return;Directory.CreateDirectory(CaptureDirectory);
        var colors=new Color[width*height];GraphicsDevice.GetBackBufferData(colors);
        using var texture=new Microsoft.Xna.Framework.Graphics.Texture2D(GraphicsDevice,width,height);texture.SetData(colors);
        using var stream=File.Create(Path.Combine(CaptureDirectory,name));texture.SaveAsPng(stream,width,height);
        if(name=="play.png")playCapture=true;
        if(name=="result.png"){smokeStage=3;resultCaptureFrame=smokeFrame;}
        if(name=="replay-result.png")smokeStage=6;
    }
}
