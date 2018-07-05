using System;
using Plugin.Media.Abstractions;

namespace IsBird.Interfaces
{
    public interface IBirdDetector
    {
        Tuple<string, string> GetBirdResults(MediaFile file);
    }
}
