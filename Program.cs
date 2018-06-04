using ClassGenerator.Generator;
using Log;
using System;
using System.Collections.Generic;
using System.Xml.Schema;

namespace ClassGenerator
{
    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
                LogManager.Init();
                log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

                var opt = new XsdContentReaderOptions();
                opt.StoreDB = true;
                opt.StoreDBPrefix = "a";

                opt.CSharpNamespace = "SampleService";

                opt.Files.Add(new XsdFileInfo { FileName = "sample.xsd", ShortNamespace = "Test" });

                var reader = new XsdContentReader();
                var content = reader.GenerateClasses(opt);
                foreach (var c in content)
                {
                    log.Debug(c);
                }
                log.Debug("All done, see generated code in log or in .cs files in Debug folder");
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
            Console.WriteLine("\nTo many text for console?\nFull log here: bin\\Debug\\log\\ClassGenerator.log");
        }

        static ILog log;
    }
}
