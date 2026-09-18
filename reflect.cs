using System;
using System.Reflection;

class Program {
    static void Main() {
        var asm = Assembly.LoadFile("/Users/yatishydv/.nuget/packages/sipsorcerymedia.ffmpeg/10.0.16/lib/net10.0/SIPSorceryMedia.FFmpeg.dll");
        var type = asm.GetType("SIPSorceryMedia.FFmpeg.FFmpegVideoEndPoint");
        Console.WriteLine(type.FullName);
        foreach (var ctor in type.GetConstructors()) {
            Console.WriteLine("ctor: " + string.Join(", ", Array.ConvertAll(ctor.GetParameters(), p => p.ParameterType.Name + " " + p.Name)));
        }
    }
}
