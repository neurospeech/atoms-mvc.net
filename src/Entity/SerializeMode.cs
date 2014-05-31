using NeuroSpeech.Atoms.Entity.Audit;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace NeuroSpeech.Atoms.Entity
{
    public enum SerializeMode
    {
        None,
        Default,
        Read,
        ReadWrite
    }
}
