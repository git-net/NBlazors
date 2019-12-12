using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NBlazors.App.Components
{
    /// <summary>
    /// 标签对。
    /// </summary>
    public interface INTag  
    {
        string TagId { get; set; }
        string TagName { get;  }
        string Class { get; set; }
        string Style { get; set; }
        ITheme Theme { get; set; }
        IJSRuntime JSRuntime { get; set; }
        IDictionary<string,object> CustomAttributes { get; set; } 
    }
    /// <summary>
    /// 泛型标签对
    /// </summary>
    /// <typeparam name="TTag">NTag的实现类</typeparam>
    /// <typeparam name="TArgs">RenderFragment<TArgs> 所需参数</typeparam>
    /// <typeparam name="TModel">通常为单个对象， viewmodel,data....</typeparam>
    public interface INTag<TTag, TArgs, TModel>:INTag 
        where TTag: INTag<TTag, TArgs, TModel>
    {
        /// <summary>
        /// 标签对之间的内容，<see cref="TArgs"/> 为参数,ChildContent 为Blazor约定名。 eg:&lt;TagName ... Context="args" &gt;内容... @args.xxxx...&lt;/TagName&gt;
        /// </summary>
        RenderFragment<TArgs> ChildContent { get; set; }
    }
    /// <summary>
    /// 支持层级结构，parent/child..
    /// </summary>
    public interface IHierarchyComponent:IDisposable
    {
        IComponent Parent { get; set; }
        IEnumerable<IComponent> Children { get;}
        void AddChild(IComponent child);
        void RemoveChild(IComponent child);
    }


}
