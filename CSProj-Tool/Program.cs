using System.Collections.Generic;
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
            if (args.Count() < 1)
            {
                Console.WriteLine("Usage: csproj-tool <csproj file> <project-references-to-remove>");
                return 1;
            }

            var projectFilePath = args[0];
            var referencesToRemove = args.Skip(1).ToList();

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

            project.Save(projectFilePath);
            return 0;
        }

        private static IEnumerable<BuildItemGroup> AllItemGroupsIn(Project project)
        {
            var collection = project.ItemGroups;
            return collection == null ? Enumerable.Empty<BuildItemGroup>() : collection.Cast<BuildItemGroup>();
        }
    }
}
