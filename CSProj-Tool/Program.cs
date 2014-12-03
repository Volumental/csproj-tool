using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            return 0;
        }

        private static IEnumerable<BuildItemGroup> AllItemGroupsIn(Project project)
        {
            var collection = project.ItemGroups;
            return collection == null ? Enumerable.Empty<BuildItemGroup>() : collection.Cast<BuildItemGroup>();
        }
    }
}
