using System;
using System.Reflection;
using SIPSorceryMedia.FFmpeg;

class Program {
    static void Main() {
        foreach (var c in typeof(FFmpegScreenSource).GetConstructors()) {
            Console.WriteLine(c);
        }
    }
}
