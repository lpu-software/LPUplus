using System;
using System.Reflection;
using SIPSorceryMedia.FFmpeg;

class Program {
    static void Main() {
        foreach (var c in typeof(FFmpegScreenSource).GetConstructors()) {
            Console.Write("FFmpegScreenSource(");
            var p = c.GetParameters();
            for(int i=0; i<p.Length; i++) {
                Console.Write(p[i].ParameterType.Name + " " + p[i].Name + (i<p.Length-1 ? ", " : ""));
            }
            Console.WriteLine(")");
        }
    }
}
