//--------------------------------------------------------------------------------------------
// Copyright 2015 Sitecore Corporation A/S
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file 
// except in compliance with the License. You may obtain a copy of the License at
//       http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the 
// License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, 
// either express or implied. See the License for the specific language governing permissions 
// and limitations under the License.
// -------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExperienceExtractor.Mapping;
using Sitecore.Install.Files;
using Sitecore.Install.Framework;
using Sitecore.Install.Metadata;

namespace ExperienceExtractor.BuildPackage
{
    class Program
    {
        static void Main(string[] args)
        {
            var outputPath = AppDomain.CurrentDomain.BaseDirectory;
                       
            var solutionRoot = new DirectoryInfo(Path.Combine(outputPath, @"..\..\.."));
            var clientRoot = new DirectoryInfo(Path.Combine(solutionRoot.FullName, "ExperienceExtractor.Client"));
            var componentAssemblies = new DirectoryInfo(Path.Combine(solutionRoot.FullName, "components"));

            var components = new HashSet<string>(componentAssemblies.GetFiles().Select(RemoveExtension));

            var blackList = new[] {"Sitecore", typeof(Program).Assembly.GetName().Name};


            var version = typeof (IFieldMapper).Assembly.GetName().Version;

            var fileVersion = string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);
                        
            var package = new FileInfo(Path.Combine(solutionRoot.FullName, "..", "build", string.Format("ExperienceExtractor-{0}.zip", fileVersion)));            
            package.Directory.Create();

            var fileRoots = new[]
            {
                new DirectoryInfo(Path.Combine(clientRoot.FullName, "App_Config\\Include\\ExperienceExtractor")),
                new DirectoryInfo(Path.Combine(clientRoot.FullName, "sitecore")),
            };
            
            using (var writer = new Sitecore.Install.Zip.PackageWriter(package.FullName))
            {
                writer.Initialize(new SimpleProcessingContext());

                var meta = new MetadataSource()
                {
                    Author = "Sitecore Corporation",
                    Publisher = "Sitecore Corporation",
                    PackageName = "Sitecore Experience Extractor",                    
                    Version = string.Format("{0}", version)
                };
                meta.Populate(writer);

                foreach (var assemblyFile in new DirectoryInfo(outputPath).GetFiles())
                {
                    if (!components.Contains(RemoveExtension(assemblyFile)) && !blackList.Any(name=>assemblyFile.Name.StartsWith(name, StringComparison.InvariantCultureIgnoreCase)))
                    {                        
                        writer.Put(new PackageEntry(new FileEntryData(Path.Combine(@"bin", assemblyFile.Name), assemblyFile)));
                    }
                }

                foreach (var dir in fileRoots)
                {
                    foreach (var file in GetFiles(dir))
                    {                    
                        writer.Put(new PackageEntry(new FileEntryData(file.FullName.Substring(clientRoot.FullName.Length + 1), file)));
                    }
                }

                writer.Finish();
            }
        }

        static IEnumerable<FileInfo> GetFiles(DirectoryInfo dir)
        {
            foreach (var file in dir.GetFiles())
            {
                yield return file;
            }

            foreach (var subfile in dir.GetDirectories().SelectMany(GetFiles))
            {
                yield return subfile;
            }
        }

        static string RemoveExtension(FileInfo file)
        {
            return file.Name.Substring(0, file.Name.Length - file.Extension.Length);
        }
    }
}
