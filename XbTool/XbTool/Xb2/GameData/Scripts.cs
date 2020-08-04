using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XbTool.BdatString;

namespace XbTool.Xb2.GameData
{
    public class Scripts
    {
        private Dictionary<int, Node> Nodes { get; } = new Dictionary<int, Node>();
        private Node this[int nodeId] => Nodes[nodeId];
        private Node BaseNode { get; }
        private List<List<Node>> Paths { get; } = new List<List<Node>>();
        private List<string> PathText = new List<string>();
        private string EventName { get; }
        private BdatStringItem BdatSource { get; set; }
        private Scripts ScriptSource { get; set; }

        public Scripts(string script_filename)
        {
            EventName = Path.GetFileNameWithoutExtension(script_filename);
            Nodes[0] = new Node(); // Base node

            var lines = File.ReadAllLines(script_filename);
            foreach (var node in lines.Select((text, idx) => new { text, idx }).Where(x => x.text.Contains("int funcNode")).Select(x => lines.Skip(x.idx).TakeWhile(y => !y.Substring(0, 1).Contains("}"))))
            {
                Node newNode = new Node(node);
                Nodes[newNode.NodeId] = newNode;
            }

            BaseNode = Nodes[int.Parse(lines.First(x => x.Contains("int now =")).Split('=')[1].Split(';')[0])];

            CleanupNodes();
            CleanupNodes();
        }

        public void CleanupNodes()
        {
            foreach (Node node in Nodes.Values)
            {
                node.Returns = node.Returns.Distinct().ToList();
            }
            foreach (Node node in Nodes.Values.Where(x => x.Returns.Count() == 1 && x.Body.Count() == 4).Reverse())
            {
                foreach (Node parent in Nodes.Values.Where(x => x.Returns.Contains(node.NodeId)))
                {
                    for (int i = 0; i < parent.Body.Count(); i++)
                        if (parent.Body[i].Contains($"return {node.NodeId}"))
                            parent.Body[i] = parent.Body[i].Replace($"return {node.NodeId}", $"return {node.Returns.First()}");

                    for (int i = 0; i < parent.Returns.Count(); i++)
                        if (parent.Returns[i] == node.NodeId)
                            parent.Returns[i] = node.Returns.First();
                }
                node.Returns = node.Returns.Distinct().ToList();
            }
        }

        public int FindPathsTo(string targetText)
        {
            var baseList = new List<Node>();
            int ret = FindPathsRecursion(targetText, new List<Node> { BaseNode });
            foreach (var path in Paths)
            {
                baseList = baseList.Union(path).ToList();
            }
            foreach (var node in baseList)
            {
                foreach (string s in node.Body)
                    PathText.Add(s);
                PathText.Add("");
            }
            return ret;
        }

        public void PrintPaths()
        {
            foreach (string s in PathText)
            {
                Console.WriteLine(s);
            }
        }

        private int FindPathsRecursion(string targetText, IEnumerable<Node> curPath)
        {
            Node lastNode = curPath.Last();

            foreach (string s in lastNode.Body)
            {
                if (s.Contains(targetText))
                {
                    Paths.Add(curPath.ToList());
                    return 1;
                }
            }

            if (lastNode.NodeId == 0)
                return 0;

            int sum = 0;

            foreach (int ret in lastNode.Returns)
            {
                sum += FindPathsRecursion(targetText, curPath.Concat(new[] { Nodes[ret] }));
            }

            return sum;
        }

        public void PrintEventTriggerConditions(string targetText, IEnumerable<string> scriptFiles, IEnumerable<BdatStringItem> listEvents)
        {
            if (FindPathsTo(targetText) == 0)
                Console.WriteLine("No paths to event target found");
            var listEvent = listEvents.FirstOrDefault(evt => evt["evtName"].DisplayString == Path.GetFileNameWithoutExtension(EventName));
            Conditions.PrintEventConditions(listEvent, scriptFiles, listEvents);
            PrintPaths();
        }


        private class Node
        {
            public int NodeId { get; }
            public List<string> Body { get; } = new List<string>();
            public List<int> Returns { get; set; } = new List<int>();
            private string[] Blacklist { get; } = { "evt_cam::", "evt_chara::", "evt_lgt", "evt_obj::", "evt_post", "evt_rand::", "evt_se::", "evt_setup::", "evt_text::", "evt_mov", "evt::" };
            public Node(IEnumerable<string> contents)
            {
                NodeId = int.Parse(contents.First().Substring(12, 2));
                foreach (string str in contents)
                {
                    bool skip = false;
                    foreach (string check in Blacklist)
                    {
                        if (str.Contains(check))
                            skip = true;
                    }
                    if (skip)
                        continue;

                    Body.Add(str);
                    if (str.Contains("return"))
                        Returns.Add(int.Parse(str.Split('n')[1].Split(';')[0]));
                }
                Body.Add("}");
            }
             
            public Node()
            {
                NodeId = 0;
            }

            public static bool operator ==(Node n1, Node n2)
            {
                return n1.NodeId == n2.NodeId;
            }
            public static bool operator !=(Node n1, Node n2)
            {
                return n1.NodeId != n2.NodeId;
            }
        }
    }
}
