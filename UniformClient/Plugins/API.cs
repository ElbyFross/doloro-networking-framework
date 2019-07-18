//Copyright 2019 Volodymyr Podshyvalov
//
//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Linq;

namespace UniformClient.Plugins
{
    /// <summary>
    /// Class that provide methods simplifying work with plugins.
    /// </summary>
    public static class API
    {
        /// <summary>
        /// Sorting items in collection by tomain recommended orders and hierarchy depth.
        /// </summary>
        /// <param name="plugins"></param>
        public static void SortByDomains(ICollection<UniformClient.Plugins.IPlugin> plugins)
        {
            var pluginsList = plugins.ToList();

            pluginsList.Sort(
                delegate (UniformClient.Plugins.IPlugin p1, UniformClient.Plugins.IPlugin p2)
                {
                    // Split fomaints to parts.
                    string[] p1DomainParts = p1.Meta.domain.Split('.');
                    string[] p2DomainParts = p2.Meta.domain.Split('.');

                    int p1DomainOrderBufer = 0;
                    int p2DomainOrderBufer = 0;

                    int maxDepth = Math.Min(p1DomainParts.Length, p2DomainParts.Length);

                    // Compare on every available depth of domain until solution.
                    for (int i = 0; i < maxDepth; i++)
                    {
                        // Try to get orders
                        if (!Int32.TryParse(p1DomainParts[i].Split('_')[0], out p1DomainOrderBufer))
                            p1DomainOrderBufer = p1DomainParts[i].GetHashCode();
                        if (!Int32.TryParse(p2DomainParts[i].Split('_')[0], out p2DomainOrderBufer))
                            p2DomainOrderBufer = p2DomainParts[i].GetHashCode();

                        // If Plugin 1 get hiether order.
                        if (p1DomainOrderBufer < p2DomainOrderBufer)
                            return -1;

                        // If Plugin 2 get hiether order.
                        if (p1DomainOrderBufer > p2DomainOrderBufer)
                            return 1;
                    }

                    // If Pligin 2 is subdomain of Pligin 1.
                    if (p2DomainParts.Length > p1DomainParts.Length)
                        return -1;

                    // If Pligin 1 is subdomain of Pligin 2.
                    if (p1DomainParts.Length > p2DomainParts.Length)
                        return 1;

                    // If domain has the a one level of the depth and has a conflicts in order or has not a declered orders, then stop sorting.
                    return 0;
                });

            // Update uniform collection.
            plugins.Clear();
            foreach (UniformClient.Plugins.IPlugin plugin in pluginsList)
            {
                plugins.Add(plugin);
            }
        }
    }
}
