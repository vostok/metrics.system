using System;
using System.Collections.Generic;

namespace Vostok.Metrics.System.Helpers
{
    #region IntendedTree usage example

    // Example input: 
    /*
        setup:
          runner: activebackup
        ports:
          eno1
            link watches:
              link summary: down
              instance[link_watch_0]:
                name: ethtool
                link: down
                down count: 0
          eno2
            link watches:
              link summary: down
              instance[link_watch_0]:
                name: ethtool
                link: down
                down count: 0
          enp4s0f0
            link watches:
              link summary: up
              instance[link_watch_0]:
                name: ethtool
                link: up
                down count: 0
          enp4s0f1
            link watches:
              link summary: up
              instance[link_watch_0]:
                name: ethtool
                link: up
                down count: 0
        runner:
          active port: enp4s0f0
    */
    // Example parameters: "ports", 2
    // Example output: 'eno1', 'eno2', 'enp4s0f0', 'enp4s0f1'

    #endregion
    
    [Obsolete]
    internal class SimplifiedIntendedTreeParser
    {
        private readonly string blockName;
        private readonly string intendation;

        public SimplifiedIntendedTreeParser(string blockName, int intendationSize)
        {
            this.blockName = blockName;
            intendation = new string(' ', intendationSize);
        }

        public IEnumerable<string> Parse(string input)
        {
            var foundBlock = false;

            foreach (var line in input.Split('\n'))
            {
                if (!foundBlock && line.StartsWith(blockName))
                    foundBlock = true;
                else if (foundBlock && line.StartsWith(intendation))
                {
                    if (line.Length > intendation.Length && line[intendation.Length] != ' ')
                        yield return line.TrimStart();
                }
                else if (foundBlock)
                    yield break;
            }
        }
    }
}