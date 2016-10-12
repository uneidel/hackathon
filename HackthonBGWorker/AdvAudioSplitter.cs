using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackthonBGWorker
{
    internal class AdvAudioSplitter
    {
        public static void OptimizeWav(string sourceFile)
        {
            StartSox(sourceFile);
            CheckFiles(Path.GetDirectoryName(sourceFile));
            ChecktoConcate(Path.GetDirectoryName(sourceFile));
        }

     
        private static void CheckFiles(string path)
        {
            foreach (var file in Directory.EnumerateFiles(path, "s*.wav"))
            {
                var foo = GetWavFileDuration(file);
                //.WriteLine($"{Path.GetFileNameWithoutExtension(file)} with length: {foo.TotalMilliseconds} ms");
                if (foo.TotalMilliseconds == 0.0)
                {
                    File.Delete(file);
                    //Console.WriteLine($"{Path.GetFileNameWithoutExtension(file)} deleted");
                }
                else if (foo.TotalMilliseconds > 20000)
                {
                    //Console.WriteLine($"{Path.GetFileNameWithoutExtension(file)} needs to be splitted again");
                    StartSmallSox(file);
                    //Console.WriteLine($"{Path.GetFileNameWithoutExtension(file)} deleted after splitting");
                    File.Delete(file);

                }

            }

        }
        private static void ChecktoConcate(string path)
        {
            Dictionary<string, double> fileList = new Dictionary<string, double>();
            foreach (var file in Directory.EnumerateFiles(path, "s*.wav"))
            {
                var foo = GetWavFileDuration(file);
                fileList.Add(Path.GetFileName(file), foo.TotalMilliseconds);
            }

            for (int i = 0; i < fileList.Count; i++)
            {
                double totallength = 0;
                var currentPos = i;
                var currentObj = fileList.ElementAt(currentPos);
                List<string> FilesToMerge = new List<string>();
                totallength = currentObj.Value;
                FilesToMerge.Add(currentObj.Key);
                while (currentPos + 1 < fileList.Count)
                {
                    var nxtObj = fileList.ElementAt(currentPos + 1);
                    var length = nxtObj.Value;
                    if ((totallength + nxtObj.Value) < 20000)
                    {
                        totallength += nxtObj.Value;
                        FilesToMerge.Add(nxtObj.Key);
                        currentPos += 1;
                    }
                    else
                    {
                        Concatenate($"File{i}.wav", FilesToMerge.ToArray(), path);
                        i = currentPos;
                        break;
                    }
                }
                if (FilesToMerge.Count > 0)
                { Concatenate($"File{i}.wav", FilesToMerge.ToArray(), path); i = currentPos; }
            }
        }

        private static async void StartSox(string fullPath)
        {
            var startInfo = new ProcessStartInfo(@"sox\\sox.exe",
                    String.Format("{0} {1} silence 0 1 0.33 1% : newfile : restart", fullPath,
                                    // String.Format("{0} {1} silence 2 0.5 0.1% 1 5 0.1% : newfile : restart", fullPath,
                                    Path.Combine(Path.GetDirectoryName(fullPath), "split.wav")));
            startInfo.UseShellExecute = false;

            var p = Process.Start(startInfo);
            p.WaitForExit();
        }
        private async static void StartSmallSox(string fullPath)
        {

            var startInfo = new ProcessStartInfo(@"sox\\sox.exe",
                   String.Format("{0} {1} silence 2 0.5 0.1% 1 5 0.1% : newfile : restart", fullPath,
                                    Path.Combine(Path.GetDirectoryName(fullPath), Path.GetFileName(fullPath))));
            startInfo.UseShellExecute = false;

            var p = Process.Start(startInfo);
            p.WaitForExit();
        }
        private static TimeSpan GetWavFileDuration(string fileName)
        {
            TimeSpan s;
            using (WaveFileReader wf = new WaveFileReader(fileName))
            {
                s = wf.TotalTime;
            };
            return s;
        }

        private static void Concatenate(string outputFile, IEnumerable<string> sourceFiles, string workingDir)
        {
            byte[] buffer = new byte[1024];
            WaveFileWriter waveFileWriter = null;

            try
            {
                foreach (string sourceFile in sourceFiles)
                {
                    using (WaveFileReader reader = new WaveFileReader(Path.Combine(workingDir, sourceFile)))
                    {
                        if (waveFileWriter == null)
                        {
                            // first time in create new Writer
                            waveFileWriter = new WaveFileWriter(Path.Combine(workingDir, outputFile), reader.WaveFormat);
                        }
                        else
                        {
                            if (!reader.WaveFormat.Equals(waveFileWriter.WaveFormat))
                            {
                                throw new InvalidOperationException("Can't concatenate WAV Files that don't share the same format");
                            }
                        }

                        int read;
                        while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            waveFileWriter.Write(buffer, 0, read);
                        }
                    }
                }
            }
            finally
            {
                if (waveFileWriter != null)
                {
                    waveFileWriter.Dispose();
                }
            }

        }
    }
}
