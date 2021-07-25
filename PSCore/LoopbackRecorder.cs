using System;
using CSCore;
using CSCore.SoundIn;
using CSCore.Codecs.WAV;
using CSCore.MediaFoundation;
using CSCore.Streams;
using CSCore.CoreAudioAPI;

namespace PSCore
{
    public enum WriterType
    {
        EncoderWriter,
        WaveWriter
    };

    public class LoopbackRecorder
    {
        public static WasapiCapture capture;
        public static SoundInSource wasapiCaptureSource;
        public static IWaveSource stereoSource;
        public static MediaFoundationEncoder encoderWriter;
        public static WaveWriter waveWriter;
        public static WriterType writerType;


        public static void StartRecording(String fileName, int bitRate = 192000)
        {
            capture = new WasapiLoopbackCapture();

            capture.Initialize();

            wasapiCaptureSource = new SoundInSource(capture);

            stereoSource = wasapiCaptureSource.ToStereo();

            switch (System.IO.Path.GetExtension(fileName))
            {
                case ".mp3":
                    encoderWriter = MediaFoundationEncoder.CreateMP3Encoder(stereoSource.WaveFormat, fileName, bitRate);
                    writerType = WriterType.EncoderWriter;
                    break;
                case ".wma":
                    encoderWriter = MediaFoundationEncoder.CreateWMAEncoder(stereoSource.WaveFormat, fileName, bitRate);
                    writerType = WriterType.EncoderWriter;
                    break;
                case ".aac":
                    encoderWriter = MediaFoundationEncoder.CreateAACEncoder(stereoSource.WaveFormat, fileName, bitRate);
                    writerType = WriterType.EncoderWriter;
                    break;
                case ".wav":
                    waveWriter = new WaveWriter(fileName, capture.WaveFormat);
                    writerType = WriterType.WaveWriter;
                    break;
            }

            switch (writerType)
            {
                case WriterType.EncoderWriter:
                    capture.DataAvailable += (s, e) =>
                    {
                        if (!SilenceBreak())
                            encoderWriter.Write(e.Data, e.Offset, e.ByteCount);
                    };
                    break;
                case WriterType.WaveWriter:
                    capture.DataAvailable += (s, e) =>
                    {
                        if (!SilenceBreak())
                            waveWriter.Write(e.Data, e.Offset, e.ByteCount);
                    };
                    break;
            }

            // Start recording
            capture.Start();
        }

        public static void StopRecording()
        {
            // Stop recording
            capture.Stop();

            // Dispose respective writers (for WAV, Dispose() writes header)
            switch (writerType)
            {
                case WriterType.EncoderWriter:
                    encoderWriter.Dispose();
                    break;
                case WriterType.WaveWriter:
                    waveWriter.Dispose();
                    break;
            }

            // Dispose of other objects
            stereoSource.Dispose();
            wasapiCaptureSource.Dispose();
            capture.Dispose();
        }

        public static bool recordedNonSilenceYet = false;
        public static int silentStopTimeout = 1500;
        public static long timeOfLastNonSilentLevel;

        private static bool SilenceBreak()
        {
            // Get the peak meter value. 
            float level = AudioMeterInformation.FromDevice(capture.Device).PeakValue;
            Console.WriteLine(level.ToString());
            // Have we recorded anything yet?
            if (recordedNonSilenceYet == false)
            {
                // No we haven't. So we're going to be patient until we do.
                // If level is > 0, then yeah, we have.
                if (level > 0)
                    recordedNonSilenceYet = true;
            } else
            {
                // Yes, we must have.
                // Is it silent now?
                if (level <= 0)
                {
                    // How long have we been silent ?
                    long elapsedTicks = DateTime.Now.Ticks - timeOfLastNonSilentLevel;
                    TimeSpan elapsedSpan = new TimeSpan(elapsedTicks);
                    Console.WriteLine(elapsedSpan.TotalMilliseconds);
                    if (elapsedSpan.TotalMilliseconds >= silentStopTimeout)
                    {
                        // That's long enough. Stop Recording.
                        StopRecording();
                        Console.WriteLine("Stopped recording. Exiting");
                        Environment.Exit(0);
                        return true;
                    }
                }
            }
            if (level > 0)
            {
                timeOfLastNonSilentLevel = DateTime.Now.Ticks;
            }
            return false;
        }
    }
}
