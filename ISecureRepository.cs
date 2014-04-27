using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech.Atoms.Mvc
{
    interface ISecureRepository  : IDisposable
    {
        IQueryable<T> Query<T>();

        BaseSecurityContext SecurityContext { get; }

        int Save();

        void Initialize(string userName, BaseSecurityContext sc);

        IDisposable CreateSecurityScope(BaseSecurityContext sc);

    }


}
