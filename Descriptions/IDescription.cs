using System;

namespace Stampsy.ImageSource
{
    public interface IDescription
    {
        Uri Url { get; set; }
        string Extension { get; }
    }
}

