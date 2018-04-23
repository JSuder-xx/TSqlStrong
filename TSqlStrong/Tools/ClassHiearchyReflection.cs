using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSqlStrong.Tools
{
    /// <summary>
    /// A non-production quality class reflector that emits a class hierarchy as indented text. This exists only as a tool to help 
    /// produce the list of all the AST classes in the TransactSQL assembly.
    /// </summary>
    public class ClassHiearchyReflection
    {
        private class ClassTreeNode
        {
            private List<ClassTreeNode> _children = new List<ClassTreeNode>();
            private string _typeName;

            public ClassTreeNode(string typeName)
            {
                this._typeName = typeName;
            }

            public string TypeName => this._typeName;

            public IEnumerable<ClassTreeNode> Children => this._children;

            public void AddChild(ClassTreeNode child)
            {
                if (child.Parent != null)
                    throw new InvalidOperationException("Class already has a parent.");

                this._children.Add(child);
                child.Parent = this;
            }

            public ClassTreeNode Parent { get; set; }
        }

        public static System.Text.StringBuilder GetClassHierarchyForAssembly(System.Reflection.Assembly assembly)
        {
            var classTypes = assembly.GetExportedTypes().Where(type => type.IsClass);
            var classTreeNodes = new Dictionary<string, ClassTreeNode>();
            foreach (Type classType in classTypes)
            {
                var newTreeNode = new ClassTreeNode(classType.Name);
                var parentIdentifier = classType.BaseType.Name;
                if (!classTreeNodes.TryGetValue(parentIdentifier, out ClassTreeNode parentNode))
                {
                    parentNode = new ClassTreeNode(parentIdentifier);
                    classTreeNodes.Add(parentIdentifier, parentNode);
                }
                classTreeNodes.Add(newTreeNode.TypeName, newTreeNode);
                parentNode.AddChild(newTreeNode);
            }

            var builder = new System.Text.StringBuilder();
            foreach (var node in classTreeNodes.Values.Where(it => it.Parent == null).OrderBy(it => it.TypeName))
            {
                Display(builder, "", node);
            }

            return builder;
        }

        private static void Write(System.Text.StringBuilder builder, string indentation, string text)
        {
            builder.AppendLine(String.Concat(indentation, text));
        }

        private static void Display(System.Text.StringBuilder builder, string indentation, ClassTreeNode node)
        {
            Write(builder, indentation, node.TypeName);
            var childIdentation = String.Concat("    ", indentation);
            foreach (var child in node.Children.OrderBy(it => it.TypeName))
            {
                Display(builder, childIdentation, child);
            }
        }
    }
}
