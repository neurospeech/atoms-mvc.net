using NeuroSpeech.WebAtoms.Entity.Audit;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core.Objects.DataClasses;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace NeuroSpeech.WebAtoms.Entity
{
    public class NavigationExpressionVisitor : ExpressionVisitor
    {


        public class ExpressionResult
        {
            public Expression Expression { get; set; }
            public object Result { get; set; }
        }


        internal Stack<ExpressionResult> ExpressionStack = new Stack<ExpressionResult>();

        internal object LastObject = null;

        public static void LoadReferences(object obj, Expression exp)
        {

            NavigationExpressionVisitor nv = new NavigationExpressionVisitor();
            nv.LastObject = obj;
            //nv.ExpressionStack.Push(obj);
            nv.Visit(exp);
        }

        protected override Expression VisitMember(MemberExpression node)
        {

            PropertyInfo p = node.Member as PropertyInfo;

            int count = ExpressionStack.Count;

            ExpressionStack.Push(new ExpressionResult { Expression = node, Result = LastObject });

            Expression result = base.VisitMember(node);

            if (p != null)
                ExpandProperty(p);

            return result;
        }

        private void ExpandProperty(PropertyInfo p)
        {
            if (p.PropertyType.IsValueType || p.PropertyType == typeof(string))
                return;

            string name = p.Name + "Reference";

            PropertyInfo px = p.DeclaringType.GetProperty(name);
            if (px == null)
                return;

            ExpressionResult currentItem = ExpressionStack.Pop();

            IRelatedEnd end = px.GetValue(currentItem.Result, null) as IRelatedEnd;
            end.Load();

            object propertyValue = p.GetValue(currentItem.Result, null);
            if (ExpressionStack.Count > 0)
            {
                ExpressionStack.Peek().Result = propertyValue;
            }
        }

    }
}
