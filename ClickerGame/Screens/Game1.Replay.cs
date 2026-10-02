// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 MoriTeahouse (森之宿茶室)
using Microsoft.Xna.Framework;
namespace ClickerGame;
public partial class Game1
{
    private bool isReplayRun;
    private readonly Dictionary<(int Lane,double Time),Queue<LinkedListNode<Note>>> replayNodes=new();
    private double RoundAccuracy=>isReplayRun?replayData?.Accuracy??0:playRun?.Accuracy??0;
    private void StartReplayView(ReplayData replay)
    {
        try
        {
            replay.Validate();
            int index=songs.FindIndex(s=>s.Id==replay.SongId);
            if(index<0)throw new InvalidDataException("Replay song is not installed.");
            currentSongIndex=index;currentDifficulty=replay.Difficulty;LoadCurrentSong();
            if(replay.BeatmapHash.Length>0&&replay.BeatmapHash!=ReplayManager.ChartHash(beatmap!))throw new InvalidDataException("Chart changed since this replay was recorded.");
        }
        catch(Exception ex)when(ex is InvalidDataException or IOException or System.Security.Cryptography.CryptographicException)
        {syncStatusText="回放載入失敗："+ex.Message;syncStatusTimer=6;return;}
        replayData=replay;replayEventIndex=0;isReplayRun=true;
        score=combo=maxCombo=hitCount=missCount=perfectCount=greatCount=goodCount=0;
        hp=GameConfig.InitialHP;hpDepleted=false;summaryShown=false;
        keyFlashes.Clear();particles.Clear();judgmentPopups.Clear();replayNodes.Clear();
        for(var node=notes.First;node!=null;node=node.Next)
        {
            var key=(node.Value.Column,(double)node.Value.Time);
            if(!replayNodes.TryGetValue(key,out var queue))replayNodes.Add(key,queue=new());
            queue.Enqueue(node);
        }
        state=GameState.ReplayView;menuMusicInstance?.Stop();LoadBeatmapMedia();BeginRound();
    }
    private void UpdateReplayView(GameTime gameTime)
    {
        if(replayData==null)return;
        double time=SongTime;
        if(!_audioStarted&&time>=0){_audioStarted=true;songTimeline.Start(HostNow);audioPlayer.Volume=(settingsManager?.Settings.MusicVolume??0.7f)*(settingsManager?.Settings.MasterVolume??0.8f);audioPlayer.Play();videoPlayer?.Play();time=0;}
        UpdateRoundEffects(Math.Min(0.05f,(float)gameTime.ElapsedGameTime.TotalSeconds));videoPlayer?.UpdateTime((float)Math.Max(0,time));
        while(replayEventIndex<replayData.Events.Count&&replayData.Events[replayEventIndex].Time<=time)
        {
            var ev=replayData.Events[replayEventIndex++];
            if(ev.NoteTime is double noteTime&&replayNodes.TryGetValue((ev.Column,noteTime),out var queue)&&queue.TryDequeue(out var matched)&&matched.List!=null)notes.Remove(matched);
            else
            {
                // Legacy records lack note identity; choose the nearest lane note without changing the saved replay.
                LinkedListNode<Note>? nearest=null;double best=double.MaxValue;
                for(var node=notes.First;node!=null;node=node.Next)
                    if(node.Value.Column==ev.Column&&Math.Abs(node.Value.Time-ev.Time)<best){best=Math.Abs(node.Value.Time-ev.Time);nearest=node;}
                if(nearest!=null)notes.Remove(nearest);
            }
            bool miss=ev.Judgment=="MISS";
            Color color=miss?new(234,128,143):ev.Judgment=="PERFECT"?Gold:Jade;
            if(miss){combo=0;missCount++;hp=Math.Max(0,hp-GameConfig.HPDrainMiss);}
            else
            {
                hp=Math.Min(100,hp+(ev.Judgment=="PERFECT"?3:ev.Judgment=="GREAT"?1.5f:0.5f));
                score+=ev.ScoreGained;combo=ev.ComboAt;maxCombo=Math.Max(maxCombo,combo);hitCount++;
                switch(ev.Judgment){case "PERFECT":perfectCount++;break;case "GREAT":greatCount++;break;default:goodCount++;break;}
                if(settingsManager?.Settings.ReducedEffects!=true)SpawnHitParticles(ev.Column);
                lastHitDelta=ev.DeltaSeconds;hitFeedbackTimer=0.7f;
            }
            judgmentPopups.Add(new(){Text=ev.Judgment,Color=color,Timer=0.55f,Position=new(LaneLeft+ev.Column*LaneWidth+LaneWidth/2,HitZoneY-88)});
        }
        if(replayEventIndex>=replayData.Events.Count&&time>=0)
        {
            audioPlayer.Stop();videoPlayer?.Stop();notes.Clear();state=GameState.Result;
            score=replayData.FinalScore;maxCombo=replayData.MaxCombo;hitCount=replayData.Hit;missCount=replayData.Miss;resultGrade=replayData.Grade;resultMenuIndex=0;
        }
    }
    private void DrawReplayView()
    {
        DrawModernGameplay(new Microsoft.Xna.Framework.GameTime());
        Label("REPLAY · "+(replayData?.Player??"guest"),35,height-48,16,Gold);
    }
}
