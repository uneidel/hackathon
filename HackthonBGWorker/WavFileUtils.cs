using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace HackthonBGWorker
{
    public static class WavFileUtils
    {

        public static void SplitAudio(string fileName)
        {
            try
            {
                var foo = WorkWithFFMPEG(fileName);
                var list = ParseOutput(foo);
                var cuepoints = CalculateCuePoints(list);
                ExecuteCuts(fileName, cuepoints);
            }
            catch (Exception ex)
            {
                var foo = ex.Message;
            }
        }

        internal static void CheckFiles(string path)
        {
            TimeSpan total = TimeSpan.FromMilliseconds(0);
            foreach (var file in Directory.EnumerateFiles(path, "s*.wav"))
            {
                var foo = GetWavFileDuration(file);
                Console.WriteLine($"{Path.GetFileNameWithoutExtension(file)} with length: {foo.TotalMilliseconds} ms");
                total += foo;
                
            }
            Console.Write($"Total Length of all Splits: {total.TotalMinutes} min");
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
        private static void ExecuteCuts(string wavFile, List<double> cuepoints)
        {
            var fileCounter = 0;
            for (int i = 0; i < cuepoints.Count; i = i + 1)
            {
                TimeSpan startspan = TimeSpan.FromSeconds(cuepoints[i]);
                TimeSpan endspan;
                if (i == cuepoints.Count - 1)
                    endspan = GetWavFileDuration(wavFile);
                else
                    endspan = TimeSpan.FromSeconds(cuepoints[i + 1]);

                WavFileUtils.TrimWavFile(wavFile, Path.Combine(Path.GetDirectoryName(wavFile), $"split{fileCounter}.wav"), startspan, endspan);
                fileCounter++;
            }
        }
        private static List<double> CalculateCuePoints(List<double> list)
        {
            List<double> cuepoints = new List<double>();

            var currentPos = list[0];
            for (int i = 1; i < list.Count - 1; i++)
            {
                if ((currentPos - list[i + 1]) < -20)
                {
                    if (currentPos < 0)
                        currentPos = currentPos * -1;
                    cuepoints.Add(currentPos);
                    currentPos = list[i];
                }
            }
            cuepoints.Add(list.Last()>0 ? list.Last() : list.Last()*-1);
            return cuepoints;
        }
        private static List<double> ParseOutput(StringBuilder sb)
        {
            String[] lines = sb.ToString().Split('\n');

            List<double> BeginSilenceList = new List<double>();
            foreach (var line in lines)
            {
                if (!line.StartsWith("[silence"))
                    continue;
                else
                {
                    var workin = line;

                    var xxx = Regex.Split(workin, "silence_start:");
                    if (xxx.Length == 1)
                        continue;
                    var xx1 = xxx[1].Replace("\r", "");
                    var num = double.Parse(xx1, CultureInfo.InvariantCulture);
                    BeginSilenceList.Add(num);
                }
            }
            return BeginSilenceList;
        }
        public static string convertToWav(string fullPath)
        {
            var fileName = Path.GetFileNameWithoutExtension(fullPath);
            var path = Path.GetDirectoryName(fullPath);
            var newFileName = $"{path}/{fileName}.wav";
            using (var reader = new MediaFoundationReader(fullPath))
            {
                WaveFileWriter.CreateWaveFile(newFileName, reader);
            }
            return newFileName;
        }
        private static StringBuilder WorkWithFFMPEG(string fullPath)
        {
            var timeout = 100000;
            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();
            using (Process process = new Process())
            {
                process.StartInfo.FileName = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), @"tools\ffmpeg.exe");
                process.StartInfo.Arguments = String.Format("-i {0} -af silencedetect=noise=-10dB:d=0.2 -f null -", fullPath);
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
                using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
                {
                    process.OutputDataReceived += (sender, e) => {
                        if (e.Data == null)
                        {
                            outputWaitHandle.Set();
                        }
                        else
                        {
                            output.AppendLine(e.Data);
                        }
                    };
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            errorWaitHandle.Set();
                        }
                        else
                        {
                            error.AppendLine(e.Data);
                        }
                    };

                    process.Start();

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    if (process.WaitForExit(timeout) &&
                        outputWaitHandle.WaitOne(timeout) &&
                        errorWaitHandle.WaitOne(timeout))
                    {
                        // Process completed. Check process.ExitCode here.
                    }
                    else
                    {
                        // Timed out.
                    }
                }
            }
            return error;
        }
        private static void TrimWavFile(string inPath, string outPath, TimeSpan cutFromStart, TimeSpan cutFromEnd)
        {
            using (WaveFileReader reader = new WaveFileReader(inPath))
            {
                using (WaveFileWriter writer = new WaveFileWriter(outPath, reader.WaveFormat))
                {
                    int bytesPerMillisecond = reader.WaveFormat.AverageBytesPerSecond / 1000;

                    int startPos = (int)cutFromStart.TotalMilliseconds * bytesPerMillisecond;
                    startPos = startPos - startPos % reader.WaveFormat.BlockAlign;

                    int endBytes = (int)cutFromEnd.TotalMilliseconds * bytesPerMillisecond;
                    endBytes = endBytes - endBytes % reader.WaveFormat.BlockAlign;
                    //int endPos = (int)reader.Length - endBytes;
                    int endPos = endBytes;

                    TrimWavFile(reader, writer, startPos, endPos);
                }
            }
        }

        private static void TrimWavFile(WaveFileReader reader, WaveFileWriter writer, int startPos, int endPos)
        {
            reader.Position = startPos;
            byte[] buffer = new byte[1024];
            while (reader.Position < endPos && reader.Position < reader.Length)
            {
                int bytesRequired = (int)(endPos - reader.Position);
                if (bytesRequired > 0)
                {
                    int bytesToRead = Math.Min(bytesRequired, buffer.Length);
                    int bytesRead = reader.Read(buffer, 0, bytesToRead);
                    if (bytesRead > 0)
                    {
                        writer.Write(buffer, 0, bytesRead);
                    }
                }
            }
        }
    }
}
