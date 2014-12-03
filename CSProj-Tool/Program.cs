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
            var projectFilePath = args[0];
            string referenceToRemove = args[1];

            var project = new Project();
            project.Load(projectFilePath);

            var collection = project.ItemGroups;
            var itemGroups = (collection == null ? Enumerable.Empty<BuildItemGroup>() : collection.Cast<BuildItemGroup>());

            var allItems = itemGroups.SelectMany(group => group.Cast<BuildItem>());
            var projectReferences = allItems.Where(IsProjectReference);

            var toRemove = projectReferences.Where(item => item.FinalItemSpec == referenceToRemove).ToList();
            
            foreach (var item in toRemove)
            {
                project.RemoveItem(item);
            }

            if (!toRemove.Any())
            {
                Console.Error.WriteLine("Could not remove '{0}' from '{1}'", referenceToRemove, projectFilePath);
                return -1;
            }

            project.Save("projectFilePath");
            return 0;
        }
    }
}
