using System;
using System.Reflection;

var asm = Assembly.LoadFile("/Users/yatishydv/.nuget/packages/sipsorcerymedia.ffmpeg/10.0.16/lib/net10.0/SIPSorceryMedia.FFmpeg.dll");
var type = asm.GetType("SIPSorceryMedia.FFmpeg.FFmpegScreenSource");
Console.WriteLine(type.FullName);
foreach (var ctor in type.GetConstructors()) {
    Console.WriteLine("ctor: " + string.Join(", ", Array.ConvertAll(ctor.GetParameters(), p => p.ParameterType.Name + " " + p.Name)));
}
