﻿using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Reflection
{
    /// <summary>
    /// Marks each type of member that is defined as a derived class of MemberInfo.
    /// </summary>
    [Flags]
    [ComVisible(true)]
    [DataContract]
    public enum MemberTypes
    {
        Constructor = 1,
        Event = 2,
        Field = 4,
        Method = 8,
        Property = 16,
        TypeInfo = 32,
        Custom = 64,
        NestedType = 128,
        All = NestedType | TypeInfo | Property | Method | Field | Event | Constructor,
    }
}
