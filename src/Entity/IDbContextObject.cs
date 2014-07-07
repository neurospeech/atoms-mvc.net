using NeuroSpeech.Atoms.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech.Atoms.Mvc.Entity
{
    public interface IRepositoryObject
    {

        Type ObjectType { get; }

    }
}
