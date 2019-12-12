using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace NBlazors.App.Components
{
    /// <summary>
    /// 抽象类NTag，<see cref="INTag{TTag, TArgs, TModel}"/> ;
    /// </summary>
    /// <typeparam name="TTag"></typeparam>
    /// <typeparam name="TArgs"></typeparam>
    /// <typeparam name="TModel"></typeparam>
    /// <remarks>
    /// 1、<see cref="OnParentChange(IComponent, IComponent)"/>
    /// </remarks>
#pragma warning disable CA1063 // Implement IDisposable Correctly
    public    abstract class AbstractNTag<TTag, TArgs, TModel> : ComponentBase,IHierarchyComponent, INTag<TTag, TArgs, TModel>
#pragma warning restore CA1063 // Implement IDisposable Correctly
        where TTag: AbstractNTag<TTag, TArgs, TModel>
    {
        List<IComponent> _children = new List<IComponent>();
        IComponent _parent;

        public string TagName => typeof(TTag).Name;
        [Inject]public IJSRuntime JSRuntime { get; set; }
        [Parameter]public RenderFragment<TArgs> ChildContent { get; set; }
        [Parameter] public string TagId { get; set; }
      
        [Parameter]public string Class { get; set; }
        [Parameter]public string Style { get; set; }
        [Parameter(CaptureUnmatchedValues =true)]public IDictionary<string, object> CustomAttributes { get; set; }
       
        [CascadingParameter] public IComponent Parent { get=>_parent; set=>_parent=OnParentChange(_parent,value); }
        [CascadingParameter] public ITheme Theme { get; set; }

        public bool TryGetAttribute(string key, out object value)
        {
            value = null;
            return CustomAttributes?.TryGetValue(key, out value) ?? false;
        }
        public IEnumerable<IComponent> Children { get=>_children;}
        protected bool FirstRender = false;
        
        protected override void OnAfterRender(bool firstRender)
        { 
            FirstRender = firstRender;
            base.OnAfterRender(firstRender); 
           
        }
        protected virtual IComponent OnParentChange(IComponent oldValue, IComponent newValue)
        { 
                ConsoleLog($"OnParentChange: {newValue}"); 
            if(oldValue is IHierarchyComponent o) o.RemoveChild(this);
            if(newValue is IHierarchyComponent n) n.AddChild(this);
            return newValue;
        }

         
        public override Task SetParametersAsync(ParameterView parameters)
        {
            return base.SetParametersAsync(parameters);
        }
        
          int logid = 0;
        public object ConsoleLog(object msg)
        {
            logid++;
            Task.Run(async ()=> await this.JSRuntime.InvokeVoidAsync("console.log", $"{TagName}[{TagId}_{ logid}:{msg}]"));
            return null;
        }


        public void AddChild(IComponent child)
        {
            this._children.Add(child);

        } 
        public void RemoveChild(IComponent child)
        {
            this._children.Remove(child);
        }
        public void Dispose()
        {
            this.Parent = null;
        }
       
       
       

        public override string ToString()
        {
            return $"{this.TagName}[{this.TagId}]";
        }

    }
}
