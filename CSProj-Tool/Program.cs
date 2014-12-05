using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml;
using Microsoft.Build.BuildEngine;
using System;

namespace csproj.tool
{
    class Program
    {
        private static bool IsProjectReference(BuildItem item)
        {
            return item.Name == "ProjectReference";
        }

        static int Main(string[] args)
        {
            if (args.Count() < 2)
            {
                Console.WriteLine("Usage: csproj-tool add|remove <csproj file> <data ...>");
                return 1;
            }
            

            if (args[0] == "remove")
            {
                var projectFilePath = args[1];
                var referencesToRemove = args.Skip(2).ToList();

                var project = new Project();
                project.Load(projectFilePath);

                var allItems = AllItemGroupsIn(project).SelectMany(group => group.Cast<BuildItem>());
                var projectReferences = allItems.Where(IsProjectReference);

                var toRemove = projectReferences.Where(item => referencesToRemove.Contains(item.FinalItemSpec)).ToList();

                foreach (var item in toRemove)
                {
                    project.RemoveItem(item);
                }

                if (!toRemove.Any())
                {
                    Console.Error.WriteLine("Could not remove '{0}' from '{1}'", referencesToRemove, projectFilePath);
                    return -1;
                }

                if (project.IsDirty)
                {
                    project.Save(projectFilePath);
                }

            }
            else if (args[0] == "add")
            {
                var projectFilePath = args[1];
                var assemblyPath = args[2];

                var project = new Project();
                project.Load(projectFilePath);

                var group = AllItemGroupsIn(project).FirstOrDefault(g => g.Cast<BuildItem>().Any(i => i.Name == "Reference")) ??
                            project.AddNewItemGroup();

                
                var item = group.AddNewItem("Reference", Path.GetFileNameWithoutExtension(assemblyPath));
                var hintPath = RelativePath.MakeRelativePath(projectFilePath, assemblyPath);
                item.SetMetadata("SpecificVersion", "False");
                item.SetMetadata("HintPath", hintPath);

                if (project.IsDirty)
                {
                    project.Save(projectFilePath);
                }
            }
            else if (args[0] == "relocate")
            {
                var projectFilePath = args[1];
                var oldPath = args[2];
                var newPath = args[3];

                var project = new Project();
                project.Load(projectFilePath);
                
                var allItems = AllItemGroupsIn(project).SelectMany(g => g.Cast<BuildItem>());
                var references = allItems.Where(i => i.Name == "Reference");

                foreach (var reference in references)
                {
                    var path = reference.GetMetadata("HintPath");
                    if (path.StartsWith(oldPath))
                    {
                        var tmp = string.Format("{0}{1}", newPath, path.Substring(oldPath.Length));
                        reference.SetMetadata("HintPath", tmp);
                    }
                }

                if (project.IsDirty)
                {
                    project.Save(projectFilePath);
                }
            }
            else if (args[0] == "nuget-add")
            {
                var xml = new XmlDocument();
                xml.Load(args[1]);
                var package = xml.CreateElement("package");
                AppendAttribute(package, "id", args[2]);
                AppendAttribute(package, "version", args[3]);
                AppendAttribute(package, "targetFramework", args[4]);

                xml.DocumentElement.AppendChild(package);
                xml.Save(args[1]);
            }
            
            return 0;
        }

        private static void AppendAttribute(XmlElement e, string name, string value)
        {
            var xml = e.OwnerDocument;
            var attribute = xml.CreateAttribute(name);
            attribute.Value = value;
            e.Attributes.Append(attribute);
        }

        private static IEnumerable<BuildItemGroup> AllItemGroupsIn(Project project)
        {
            var collection = project.ItemGroups;
            return collection == null ? Enumerable.Empty<BuildItemGroup>() : collection.Cast<BuildItemGroup>();
        }
    }
}
