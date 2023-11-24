using System.Runtime.InteropServices;
using SoundpadConnector;
using SoundpadConnector.XML;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;

namespace YT2PAD
{
    internal class Program
    {
        static YoutubeDL ytdl = new YoutubeDL(16);
        public static Soundpad Soundpad = new Soundpad();
        static string AppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).ToString() + "\\space.sliced\\YT2PAD";
        static async Task Main(string[] args)
        {
            await GetSetup();
            while(true)
            {
                Console.WriteLine("\nInput Youtube URL to download audio or playlist or \"exit\" to stop the program.");
                string url = Console.ReadLine()??"";
                if(url=="exit")
                {
                    Environment.Exit(0);
                }
                if(!url.StartsWith("https://www.youtube.com/")&&!url.StartsWith("https://youtu.be/")&&!url.StartsWith("https://youtube.com/"))
                {
                    Console.WriteLine("Not a youtube link.");
                    continue;
                }
                if(url.Contains("list="))
                {
                    url = url.Split("list=")[1].Split("&")[0];
                    Console.WriteLine("Starting playlist download, this might take a while...");
                    await GetPlaylist(url);
                }else{
                    Console.WriteLine("Starting video download, this might take a while...");
                    await GetVideo(url);
                }
            }
            
        }


        private static async Task GetSetup()
        {
            Directory.CreateDirectory(AppData);
            Console.WriteLine("Appdata(ffmeg, yt-dlp, and audios) can be found at:\n"+AppData+"\n");
            if(!File.Exists(AppData+"\\ffmpeg.exe")){
                Console.WriteLine("Downloading Dependecies\n");
                await Utils.DownloadYtDlp(AppData);
                await Utils.DownloadFFmpeg(AppData);
            }
            ytdl.YoutubeDLPath = AppData + "\\yt-dlp.exe";
            ytdl.FFmpegPath = AppData + "\\ffmpeg.exe";
            Directory.CreateDirectory(AppData+"\\Playlists");
            await Soundpad.ConnectAsync();
            var temp = await Soundpad.GetCategories();
            var searchlist = temp.Value.Categories;
            bool plexists=false;
            bool soexists=false;
            foreach(Category cg in searchlist)
            {
                if(cg.Name.ToLower()=="yt2pad-playlists")
                {
                    plexists=true;
                }
                if(cg.Name.ToLower()=="yt2pad-audio")
                {
                    soexists=true;
                }
            }
            if(!plexists)
            {
                await Soundpad.AddCategory("yt2pad-playlists", -1);
            }
            if(!soexists)
            {
                await Soundpad.AddCategory("yt2pad-audio", -1);
            }
            Console.WriteLine("Startup done\n");
        }


        private static async Task GetVideo(string Video)
        {
            ytdl.OutputFolder = AppData + "\\Audios";
            var res = await ytdl.RunAudioDownload(Video, format: AudioConversionFormat.Mp3, overrideOptions: new OptionSet{RestrictFilenames = true});
            var temp = await Soundpad.GetCategories();
            var searchlist = temp.Value.Categories;
            int target=-1;
            foreach(Category cg in searchlist)
            {
                if(cg.Name.ToLower()=="yt2pad-audio")
                {
                    target = cg.Index;
                    continue;
                }
            }
            await Soundpad.RemoveCategory(target);
            Thread.Sleep(750);
            await Soundpad.AddCategory("yt2pad-audio");
            Thread.Sleep(750);
            temp = await Soundpad.GetCategories();
            searchlist = temp.Value.Categories;
            foreach(Category cg in searchlist)
            {
                Console.WriteLine(cg.Name+" - "+cg.Index);
                if(cg.Name.ToLower()=="yt2pad-audio")
                {
                    target = cg.Index;
                    continue;
                }
            }
            
            Thread.Sleep(1000);
            foreach(var i in Directory.GetFiles(ytdl.OutputFolder))
            {
                await Soundpad.AddSound(i, 1, target);
                Thread.Sleep(500);
            }
        }


        private static async Task GetPlaylist(string Playlist)
        {
            
            var OutputFolder = AppData + "\\Playlists\\"+Playlist;
            ytdl.OutputFolder = OutputFolder;
            if(Directory.Exists(OutputFolder)){
                Console.WriteLine("Playlist already exists\nTo redownload it, delete the existing folder in Appdata: "+OutputFolder);
                return;
            }
            var temp = await Soundpad.GetCategories();
            var searchlist = temp.Value.Categories;
            int target=-1;
            foreach(Category cg in searchlist)
            {
                if(cg.Name.ToLower()=="yt2pad-playlists")
                {
                    target = cg.Index;
                }
            }
            var url = "https://www.youtube.com/playlist?list="+Playlist;
            await ytdl.RunAudioPlaylistDownload(url, format: AudioConversionFormat.Mp3, overrideOptions: new OptionSet{RestrictFilenames = true});
            await Soundpad.AddCategory(Playlist, target);
            temp = await Soundpad.GetCategories();
            searchlist = temp.Value.Categories;
            target=-1;
            bool SawPlaylists=false;
            foreach(Category cg in searchlist)
            {
                
                Console.WriteLine(cg.Name+" - "+cg.Index);
                if(cg.Name.ToLower()=="yt2pad-playlists")
                {
                    SawPlaylists =true;
                }else if(SawPlaylists)
                {
                    target=cg.Index-1;
                }
            }
            
            foreach(var file in Directory.GetFiles(OutputFolder))
            {
                Console.WriteLine(file);
                await Soundpad.AddSound(file, 1, target);
                Thread.Sleep(500);
            }
        }
    }
}
