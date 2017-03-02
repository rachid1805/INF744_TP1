using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLinkApplication
{
  class Program : IDisposable
  {
    static int Main(string[] args)
    {
      DataLinkController._dataLinkController = new DataLinkController();

      return DataLinkController._dataLinkController.Process(args);
    }

    public void Dispose()
    {
      ((IDisposable)DataLinkController._dataLinkController).Dispose();
    }
  }
}
