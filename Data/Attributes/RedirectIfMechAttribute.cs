using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XansCharacter.Data.Attributes {

	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
	public sealed class RedirectIfMechAttribute : Attribute {
	}
}
