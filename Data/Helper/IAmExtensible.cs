using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DreamsOfInfiniteGlass.Data.Helper {
	internal interface IAmExtensible<TExtensible> {

		TExtensible Get(object from);

	}
}
