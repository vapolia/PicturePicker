using System;

namespace Vapolia.Mvvmcross.PicturePicker
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Delegate | AttributeTargets.Enum | AttributeTargets.Event | AttributeTargets.Field | AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Struct)]
    internal sealed class PreserveAttribute : Attribute
    {
        public bool AllMembers;
        //public bool Conditional;
    }
}