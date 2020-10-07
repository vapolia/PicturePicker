using System;

namespace Vapolia.PicturePicker
{
    internal  sealed class PreserveAttribute : Attribute
    {
        public bool AllMembers;
        public bool Conditional;
    }
}