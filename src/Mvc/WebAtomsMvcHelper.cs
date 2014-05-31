using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Serialization;
using System.Web.Script.Serialization;
using NeuroSpeech.Atoms.Mvc;
using NeuroSpeech.Atoms.Entity;

namespace NeuroSpeech.Atoms
{

    public interface IJavaScriptSerializer {
        string Serialize(object obj);
    }

	public interface IJavaScriptSerializable {
		string Serialize(IJavaScriptSerializer js);
	}

	public interface IJavaScriptDeserializable {
		void Deserialize(Dictionary<string, object> values);
	}
}
