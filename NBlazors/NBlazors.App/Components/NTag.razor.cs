using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
namespace NBlazors.App.Components
{
    public partial class NTag<TModel> : AbstractNTag<NTag<TModel>, RenderArgs<NTag<TModel>, TModel>, TModel>
    {
        [Parameter] public TModel Model { get; set; }
        [Parameter] public NVisibility TextVisibility { get; set; } = NVisibility.Default;
        [Parameter] public bool ShowContent { get; set; } = true;

        [Parameter] public Func<TModel, object, object> Getter { get; set; }
        [Parameter] public string Text { get; set; }
       
        
         public RenderFragment RenderText()
        {
            if (TextVisibility == NVisibility.Hidden|| string.IsNullOrEmpty(this.Text)) return null;
            if (TextVisibility == NVisibility.Markup) return (b) => b.AddMarkupContent(0,  this.Text);
            return (b) => b.AddContent(0, Text);

        }
        public RenderFragment RenderContent(RenderArgs<NTag<TModel>, TModel> args)
        {
           return   this.ChildContent?.Invoke(args) ;
        }
        public RenderFragment RenderContent(object arg=null)
        {
            return this.RenderContent(this.Args(arg));
        }

        public RenderArgs<NTag<TModel>, TModel> Args(object arg = null)
        {

            return new RenderArgs<NTag<TModel>, TModel>(this, this.Model, arg,this.Getter);
        }
        public RenderArgs<NTag<TModel>, TModel> Args(TModel model, object arg = null)
        {

            return new RenderArgs<NTag<TModel>, TModel>(this, model, arg, this.Getter);
        }


        public RenderFragment RenderChildren(TModel model, object arg=null)
        {
            return (builder) =>
            {
                var children = this.Children.OfType<NTag<TModel>>();
                NTag<TModel> defaultTag = null;
                foreach (var child in children)
                {
                    if (defaultTag == null && child.ChildContent != null) defaultTag = child;
                    var render = (child.ChildContent == null ? defaultTag : child);
                    render.RenderContent(child.Args(model, arg))(builder);
                }
            };
            
        }
        
        public override string ToString()
        {
            return $"{this.TagName}<{typeof(TModel).Name}>[{this.TagId},{Model}]";
        }
    }

    public enum NVisibility
    {
        Default,
        Markup,
        Hidden 
    }
}
