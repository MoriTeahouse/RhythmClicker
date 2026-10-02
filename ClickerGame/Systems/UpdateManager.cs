// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 MoriTeahouse (森之宿茶室)
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using MatrixTea.Engine.Core.IO;
namespace ClickerGame.Systems;
public static class UpdateManager
{
    public const string ManifestUrl="https://raw.githubusercontent.com/MoriTeahouse/RhythmClicker/main/version.json";
    public static bool IsUpdateAvailable{get;private set;}
    public static string? AvailableVersion{get;private set;}
    public static string? DownloadUrl{get;private set;}
    public static bool IsDownloading{get;private set;}
    public static float DownloadProgress{get;private set;}
    public static string StatusText{get;private set;}="";
    public static string? ReadyExecutable{get;private set;}
    private static string? checksum;
    private static readonly SemaphoreSlim DownloadGate=new(1,1);
    public static event Action? UpdateAvailable;
    public static event Action? UpdateReady;
    public static void CheckAsync()=>_ = Task.Run(async()=>
    {
        try
        {
            using var http=Client();http.Timeout=TimeSpan.FromSeconds(10);
            var manifest=JsonSerializer.Deserialize<VersionManifest>(await http.GetStringAsync(ManifestUrl));
            if(manifest==null||!Version.TryParse(manifest.Version?.TrimStart('v'),out var remote)||!Version.TryParse(Core.AppPaths.ReadCurrentVersion().TrimStart('v'),out var current)||remote<=current)return;
            if(!IsTrustedReleaseUrl(manifest.DownloadUrl)||manifest.Sha256?.Length!=64||!manifest.Sha256.All(Uri.IsHexDigit))return;
            AvailableVersion=remote.ToString();DownloadUrl=manifest.DownloadUrl;checksum=manifest.Sha256;IsUpdateAvailable=true;UpdateAvailable?.Invoke();
        }
        catch(HttpRequestException){}catch(TaskCanceledException){}catch(JsonException){}
    });
    public static bool IsTrustedReleaseUrl(string? url)=>Uri.TryCreate(url,UriKind.Absolute,out var uri)&&uri.Scheme=="https"&&uri.Host.Equals("github.com",StringComparison.OrdinalIgnoreCase)&&uri.AbsolutePath.StartsWith("/MoriTeahouse/RhythmClicker/releases/download/",StringComparison.OrdinalIgnoreCase)&&uri.UserInfo.Length==0;
    /// <summary>Compatibility API: stages a verified portable build; switching versions is explicit.</summary>
    public static async Task DownloadAndInstallAsync()
    {
        if(!await DownloadGate.WaitAsync(0))return;
        try
        {
            if(!IsTrustedReleaseUrl(DownloadUrl)||checksum==null||AvailableVersion==null)return;
            IsDownloading=true;DownloadProgress=0;StatusText="正在下載更新…";
            string folder=Path.Combine(Core.AppPaths.InstallRoot,"Updates",AvailableVersion+"-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);string archive=Path.Combine(folder,"release.zip");
            using var http=Client();http.Timeout=TimeSpan.FromMinutes(10);
            using(var response=await http.GetAsync(DownloadUrl,HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();Uri? final=response.RequestMessage?.RequestUri;
                if(final?.Scheme!="https"||!(final.Host=="github.com"||final.Host.EndsWith(".githubusercontent.com",StringComparison.OrdinalIgnoreCase)))throw new InvalidDataException("Untrusted release redirect.");
                long size=response.Content.Headers.ContentLength??0;
                if(size>1024L*1024*1024)throw new InvalidDataException("Release download exceeds limit.");
                using var input=await response.Content.ReadAsStreamAsync();using var output=File.Create(archive);
                byte[] buffer=new byte[81920];long received=0;int count;
                while((count=await input.ReadAsync(buffer))>0)
                {
                    received+=count;if(received>1024L*1024*1024)throw new InvalidDataException("Release download exceeds limit.");
                    await output.WriteAsync(buffer.AsMemory(0,count));DownloadProgress=size>0?(float)received/size*0.8f:0;
                }
                await output.FlushAsync();
            }
            StatusText="正在驗證更新…";
            ReadyExecutable=VerifiedUpdatePackage.Stage(archive,Path.Combine(folder,"Game"),checksum);
            AtomicFile.WriteText(Path.Combine(folder,"ready.json"),JsonSerializer.Serialize(new{version=AvailableVersion,executable=ReadyExecutable,data_root=Core.AppPaths.InstallRoot,sha256=checksum}));
            DownloadProgress=1;StatusText="更新已準備完成。按 U 啟動新版本。";UpdateReady?.Invoke();
        }
        catch(Exception ex)when(ex is IOException or HttpRequestException or TaskCanceledException or UnauthorizedAccessException)
        {StatusText="更新失敗："+ex.Message;ReadyExecutable=null;}
        finally{IsDownloading=false;DownloadGate.Release();}
    }
    public static bool ShowRestartDialog(string yes,string no,string message,string title){StatusText=message;return false;}
    public static void RestartGame()
    {
        if(ReadyExecutable==null||!File.Exists(ReadyExecutable))return;
        var start=new ProcessStartInfo(ReadyExecutable){UseShellExecute=false,WorkingDirectory=Path.GetDirectoryName(ReadyExecutable)!};
        start.ArgumentList.Add("--data-root");start.ArgumentList.Add(Core.AppPaths.InstallRoot);
        Process.Start(start);Environment.Exit(0);
    }
    private static HttpClient Client(){var client=new HttpClient();client.DefaultRequestHeaders.UserAgent.ParseAdd("RhythmClicker-Updater/0.6.0");return client;}
    private sealed class VersionManifest
    {
        [JsonPropertyName("version")]public string? Version{get;set;}
        [JsonPropertyName("download_url")]public string? DownloadUrl{get;set;}
        [JsonPropertyName("sha256")]public string? Sha256{get;set;}
    }
}
