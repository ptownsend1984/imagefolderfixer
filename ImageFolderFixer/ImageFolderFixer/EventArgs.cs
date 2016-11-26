using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ImageFolderFixer
{
    public class EventArgs<T> : EventArgs
    {

        public T Data { get; }
        public EventArgs(T data)
        {
            Data = data;
        }

    }
}