通过一个小组件，熟悉Blazor服务端组件开发。
## 一、环境搭建
 vs2019 16.4, asp.net core 3.1 新建Blazor应用，选择 asp.net core 3.1。 根文件夹下新增目录Components，放置代码。
## 二、组件需求定义
 
Components目录下新建一个接口文件当作文档,加个using `using Microsoft.AspNetCore.Components;`。

先从直观的方面入手。
* 类似html标签对的组件,样子类似`<xxx propA="aaa" data-propB="123" ...>其他标签或内容...</xxx>`或`<xxx .../>`。接口名：INTag.
* 需要Id和名称，方便区分和调试。`string TagId{get;set;} string TagName{get;set;}`.
* 需要样式支持。加上`string Class{get;set;} string Style{get;set;}`。
* 不常用的属性也提供支持,使用字典。` IDictionary<string,object> CustomAttributes { get; set; }`
* 应该提供js支持。加上`using Microsoft.JSInterop;` 属性 `IJSRuntime JSRuntime{get;set;}` 。

考虑一下功能方面。
* 既然是标签对，那就有可能会嵌套，就会产生层级关系或父子关系。因为只是可能，所以我们新建一个接口，用来提供层级关系处理，IHierarchyComponent。
* 需要一个Parent ，类型就定为Microsoft.AspNetCore.Components.IComponent.`IComponent Parent { get; set; }`.
* 要能添加子控件，`void AddChild(IComponent child);`,有加就有减,`void RemoveChild(IComponent child);`。
* 提供一个集合方便遍历，我们已经提供了Add/Remove,让它只读就好。 `IEnumerable<IComponent> Children { get;}`。
* 一旦有了Children集合，我们就需要考虑什么时候从集合里移除组件，让IHierarchyComponent实现IDisposable,保证组件被释放时解开父子/层级关系。
* 组件需要处理样式，仅有Class和Style可能不够，通常还会需要Skin、Theme处理，增加一个接口记录一下， `public interface ITheme{ string GetClass<TComponent>(TComponent component); }`。INTag增加一个属性 `ITheme Theme { get; set; }`


INTag:
```
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
```

IHierarchyComponent:
```
 public interface IHierarchyComponent:IDisposable
    {
        IComponent Parent { get; set; }
        IEnumerable<IComponent> Children { get;}
        void AddChild(IComponent child);
        void RemoveChild(IComponent child);
    }
```

ITheme
```
 public interface ITheme
    { 
        string GetClass<TComponent>(TComponent component);
    }
```



组件的基本信息 INTag 有了，需要的话可以支持层级关系 IHierarchyComponent，可以考虑下一些特定功能的处理及类型部分。
* Blazor组件实现类似 `<xxx>....</xxx>`这种可打开的标签对，需要提供一个 `RenderFragment 或 RenderFragment<TArgs>`属性。RenderFragment是一个委托函数，带参的明显更灵活些，但是参数类型不好确定，不好确定的类型用泛型。再加一个接口，`INTag< TArgs >:INTag`, 一个属性 `RenderFragment<TArgs> ChildContent { get; set; }`.
* 组件的主要目的是为了呈现我们的数据，也就是一般说的xxxModel,Data....,类型不确定，那就加一个泛型。`INTag< TArgs ,TModel>:INTag`.
* RenderFragment是一个函数，ChildContent 是一个函数属性，不是方法。在方法内，我们可以使用this来访问组件自身引用，但是函数内部其实是没有this的。为了更好的使用组件自身，这里增加一个泛型用于指代自身，`    public interface INTag<TTag, TArgs, TModel>:INTag     where TTag: INTag<TTag, TArgs, TModel>`。

INTag[TTag, TArgs, TModel ]
```
 public interface INTag<TTag, TArgs, TModel>:INTag 
        where TTag: INTag<TTag, TArgs, TModel>
    {
        /// <summary>
        /// 标签对之间的内容，<see cref="TArgs"/> 为参数,ChildContent 为Blazor约定名。
        /// </summary>
        RenderFragment<TArgs> ChildContent { get; set; }
    }
```




回顾一下我们的几个接口。
* INTag:描述了组件的基本信息，即组件的样子。
* IHierarchyComponent 提供了层级处理能力，属于组件的扩展能力。
* ITheme 提供了Theme接入能力，也属于组件的扩展能力。
* INTag<TTag, TArgs, TModel> 提供了打开组件的能力，ChildContent像一个动态模板一样，让我们可以在声明组件时自行决定组件的部分内容和结构。
* 所有这些接口最主要的目的其实是为了产生一个合适的TArgs， 去调用ChildContent。
* 有描述，有能力还有了主要目的，我们就可以去实现NTag组件。


## 三、组件实现

### 抽象基类 AbstractNTag

Components目录下新增 一个c#类，AbstractNTag.cs, `using Microsoft.AspNetCore.Components;` 借助Blazor提供的ComponentBase,实现接口。
```
public    abstract class AbstractNTag<TTag, TArgs, TModel> : ComponentBase, IHierarchyComponent, INTag<TTag, TArgs, TModel>
   where TTag: AbstractNTag<TTag, TArgs, TModel>{  

}
```

调整一下vs生成的代码， IHierarchyComponent 使用字段实现一下。

Children:
```
 List<IComponent> _children = new List<IComponent>();
       
 public void AddChild(IComponent child)
        {
            this._children.Add(child);

        } 
        public void RemoveChild(IComponent child)
        {
            this._children.Remove(child);
        } 

```
 
Parent,dispose
```
 IComponent _parent;
public IComponent Parent { get=>_parent; set=>_parent=OnParentChange(_parent,value); }
 protected virtual IComponent OnParentChange(IComponent oldValue, IComponent newValue)
        { 
                 
            if(oldValue is IHierarchyComponent o) o.RemoveChild(this);
            if(newValue is IHierarchyComponent n) n.AddChild(this);
            return newValue;
        }
public void Dispose()
        {
            this.Parent = null;
        }

```
 
增加对浏览器console.log 的支持, razor Attribute...,完整的AbstractNTag.cs
```
public    abstract class AbstractNTag<TTag, TArgs, TModel> : ComponentBase, IHierarchyComponent, INTag<TTag, TArgs, TModel>
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
       
        protected virtual IComponent OnParentChange(IComponent oldValue, IComponent newValue)
        { 
                ConsoleLog($"OnParentChange: {newValue}"); 
            if(oldValue is IHierarchyComponent o) o.RemoveChild(this);
            if(newValue is IHierarchyComponent n) n.AddChild(this);
            return newValue;
        }

        protected bool FirstRender = false;
        
        protected override void OnAfterRender(bool firstRender)
        { 
            FirstRender = firstRender;
            base.OnAfterRender(firstRender); 
           
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
}
```

* Inject 用于注入
* Parameter 支持组件声明的Razor语法中直接赋值，<NTag Class="ssss" .../>;
* `Parameter(CaptureUnmatchedValues =true)` 支持声明时将组件上没定义的属性打包赋值;
* `CascadingParameter` 配合Blazor 内置组件 `<CascadingValue Value="xxx" >... <NTag /> ...</CascadingValue>`,捕获Value。处理过程和级联样式表（css）很类似。

### 具体类 NTag

泛型其实就是定义在类型上的函数，`TTag,TArgs,TModel` 就是 入参，得到的类型就是返回值。因此处理泛型定义的过程，就很类似函数逐渐消参的过程。比如：
```
func(a,b,c) 
  确定a之后，func(b,c)=>func(1,b,c);
  确定b之后，func(c)=>func(1,2,c);
  最终： func()=>func(1,2,3)；
  执行 func 可以得到一个明确的结果。
```

同样的,我们继承NTag基类时需要考虑各个泛型参数应该是什么：
* TTag:这个很容易确定，谁继承了基类就是谁。
* TModel: 这个不到最后使用我们是无法确定的，需要保留。
* TArgs: 前面说过，组件的主要目的是为了给ChildContent 提供参数.从这一目的出发，TTag和TModel的用途之一就是给`TArgs`提供类型支持,或者说TArgs应该包含TTag和TModel。又因为ChildContent只有一个参数，因此TArgs应该有一定的扩展性，不妨给他一个属性做扩展。 综合一下，TArgs的大概模样就有了,来个struct。
```
public struct RenderArgs<TTag,TModel>   
    {
        public TTag Tag;
        public TModel Model;
        public object Arg;
        
        public RenderArgs(TTag tag, TModel model, object arg  ) {
            this.Tag = tag;
            this.Model = model;
            this.Arg = arg;
           
        }
    }
```


* RenderArgs 属于常用辅助类型，因此不需要给TArgs 指定约束。

Components目录下新增Razor组件，NTag.razor;aspnetcore3.1 组件支持分部类，新增一个NTag.razor.cs;

NTag.razor.cs 就是标准的c#类写法
```
public partial  class NTag< TModel> :AbstractNTag<NTag<TModel>,RenderArgs<NTag<TModel>,TModel>,TModel> 
    {
        [Parameter]public TModel Model { get; set; }

        public RenderArgs<NTag<TModel>, TModel> Args(object arg=null)
        {
           
            return new RenderArgs<NTag<TModel>, TModel>(this, this.Model, arg);
        }
    }
```

重写一下NTag的ToString,方便测试

```
public override string ToString()
        {
            return $"{this.TagName}<{typeof(TModel).Name}>[{this.TagId},{Model}]";
        }
```


NTag.razor
```
@typeparam TModel
@inherits AbstractNTag<NTag<TModel>,RenderArgs<NTag<TModel>,TModel>,TModel>//保持和NTag.razor.cs一致
   @if (this.ChildContent == null)
        {
            <div>@this.ToString()</div>//默认输出，用于测试
        }
        else
        {
            @this.ChildContent(this.Args());
        }
@code {

}
```

简单测试一下, 数据就用项目模板自带的Data 打开项目根目录，找到`_Imports.razor`,把using 加进去 
```
@using xxxx.Data
@using xxxx.Components
```

 新增Razor组件【Test.razor】
```
未打开的NTag,输出NTag.ToString():
<NTag TModel="object" />
打开的NTag:
<NTag Model="TestData" Context="args" > 
        <div>NTag内容 @args.Model.Summary; </div>
</NTag>

<NTag Model="@(new {Name="匿名对象" })" Context="args">
    <div>匿名Model,使用参数输出【Name】属性: @args.Model.Name</div>
</NTag>

@code{
WeatherForecast TestData = new WeatherForecast { TemperatureC = 222, Summary = "aaa" };
}
```

转到Pages/Index.razor, 增加一行`<Test />`,F5 。

### 应用级联参数 CascadingValue/CascadingParameter

我们的组件中Theme 和Parent 被标记为【CascadingParameter】，因此需要通过CascadingValue把值传递过来。

首先，修改一下测试组件,使用嵌套NTag,描述一个树结构，Model值指定为树的Level。
```
 <NTag Model="0" TagId="root" Context="root">
        <div>root.Parent:@root.Tag.Parent  </div>
        <div>root Theme:@root.Tag.Theme</div>
       
        <NTag TagId="t1" Model="1" Context="t1">
            <div>t1.Parent：@t1.Tag.Parent  </div>
            <div>t1 Theme:@t1.Tag.Theme</div>
            <NTag TagId="t1_1" Model="2" Context="t1_1">
                <div>t1_1.Parent：@t1_1.Tag.Parent  </div>
                <div>t1_1 Theme：@t1_1.Tag.Theme </div>
                <NTag TagId="t1_1_1" Model="3" Context="t1_1_1">
                    <div>t1_1_1.Parent：@t1_1_1.Tag.Parent </div>
                    <div>t1_1_1 Theme：@t1_1_1.Tag.Theme </div>
                </NTag>
                <NTag TagId="t1_1_2" Model="3" Context="t1_1_2">
                    <div>t1_1_2.Parent：@t1_1_2.Tag.Parent</div>
                    <div>t1_1_2 Theme：@t1_1_2.Tag.Theme </div>
                </NTag>
            </NTag>
        </NTag>
      
    </NTag>
```


1、 Theme:Theme 的特点是共享，无论组件在什么位置，都应该共享同一个Theme。这类场景，只需要简单的在组件外套一个CascadingValue。
```
<CascadingValue Value="Theme.Default"> 
<NTag  TagId="root" ......
</CascadingValue>

```

F5跑起来，结果大致如下： 

<div>root.Parent:  </div> 
        <div>root Theme:Theme[blue]</div> 
       
            <div>t1.Parent：  </div> 
            <div>t1 Theme:Theme[blue]</div> 
        
                <div><!--!-->t1_1.Parent：  </div><!--!-->
                <div><!--!-->t1_1 Theme：Theme[blue] </div><!--!-->
           
                    <div><!--!-->t1_1_1.Parent： </div><!--!-->
                    <div><!--!-->t1_1_1 Theme：Theme[blue] </div><!--!-->
         
                    <div><!--!-->t1_1_2.Parent：</div><!--!-->
                    <div><!--!-->t1_1_2 Theme：Theme[blue] </div><!--!-->


2、Parent:Parent和Theme不同，我们希望他和我们组件的声明结构保持一致，这就需要我们在每个NTag内部增加一个CascadingValue，直接写在Test组件里过于啰嗦了，让我们调整一下NTag代码。打开NTag.razor,修改一下,Test.razor不动。
```
  <CascadingValue Value="this"> 
        @if (this.ChildContent == null)
        {
            <div>@this.ToString()</div>//默认输出，用于测试
        }
        else
        {
            @this.ChildContent(this.Args());
        }
     </CascadingValue> 
```
 看一下结果
 
<div>root.Parent:  </div><!--!-->
        <div>root Theme:Theme[blue]</div><!--!-->
        <!--!--><!--!--><!--!--> 
<!--!-->
            <div><!--!-->t1.Parent：NTag`1[root,0]  </div><!--!-->
            <div>t1 Theme:Theme[blue]</div><!--!-->
            <!--!--><!--!--><!--!--> 
<!--!-->
                <div><!--!-->t1_1.Parent：NTag`1[t1,1]  </div><!--!-->
                <div><!--!-->t1_1 Theme：Theme[blue] </div><!--!-->
                <!--!--><!--!--><!--!--> 
<!--!-->
                    <div><!--!-->t1_1_1.Parent：NTag`1[t1_1,2] </div><!--!-->
                    <div><!--!-->t1_1_1 Theme：Theme[blue] </div><!--!-->
                     <!--!-->
                <!--!--><!--!--><!--!--> 
<!--!-->
                    <div><!--!-->t1_1_2.Parent：NTag`1[t1_1,2]</div><!--!-->
                    <div><!--!-->t1_1_2 Theme：Theme[blue] </div><!--!-->

* CascadingValue/CascadingParameter 除了可以通过类型匹配之外还可以指定Name。


### 呈现Model
到目前为止，我们的NTag主要在处理一些基本功能，比如隐式的父子关系、子内容ChildContent、参数、泛型。。接下来我们考虑如何把一个Model呈现出来。<br/>
对于常见的Model对象来说，呈现Model其实就是把Model上的属性、字段。。。这些成员信息呈现出来，因此我们需要给NTag增加一点能力。

* 描述成员最直接的想法就是lambda，model=>model.xxxx，此时我们只需要Model就足够了；
* UI呈现时仅有成员还不够，通常会有格式化需求，比如：{0:xxxx}； 或者带有前后缀： "￥{xxxx}元整"，甚至就是一个常量。。。。此类信息通常应记录在组件上，因此我们需要组件自身。
* 呈现时有时还会用到一些环境变量，比如序号/行号这种，因此需要引入一个参数。
* 以上需求可以很容易的推导出一个函数类型：Func<TTag, TModel,object,object> ；考虑TTag就是组件自身，这里可以简化一下：Func<TModel,object,object>。 主要目的是从model上取值，兼顾格式化及环境变量处理，返回结果会直接用于页面呈现输出。

调整下NTag代码，增加一个类型为Func<TModel,TArg,object> 的Getter属性,打上【Parameter】标记。
```
[Parameter]public Func<TModel,object,object> Getter { get; set; }
```

* 此处也可使用表达式(Expression<Func<TModel,object,object>>)，需要增加一些处理。
* 呈现时通常还需要一些文字信息，比如 lable，text之类， 支持一下；

```
  [Parameter] public string Text { get; set; }
```

* UI呈现的需求难以确定，通常还会有对状态的处理， 这里提供一些辅助功能就可以。 

一个小枚举
```
   public enum NVisibility
    {
        Default,
        Markup,
        Hidden 
    }

```
状态属性和render方法,NTag.razor.cs
```
         [Parameter] public NVisibility TextVisibility { get; set; } = NVisibility.Default;
        [Parameter] public bool ShowContent { get; set; } = true;

 public RenderFragment RenderText()
        {
            if (TextVisibility == NVisibility.Hidden|| string.IsNullOrEmpty(this.Text)) return null;
            if (TextVisibility == NVisibility.Markup) return (b) => b.AddContent(0, (MarkupString)Text);
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
```

NTag.razor
```
   <CascadingValue Value="this">
        @RenderText()
        @if (this.ShowContent)
        {
            var render = RenderContent();
            if (render == null)
            {
                <div>@this</div>//测试用
            }
            else
            {
                @render//render 是个函数，使用@才能输出，如果不考虑测试代码，可以直接 @RenderContent()
            }
             
        }
    </CascadingValue> 

```

Test.razor增加测试代码

```
7、呈现Model
<br />
value:@@arg.Tag.Getter(arg.Model,null)
<br />
<NTag Text="日期" Model="TestData" Getter="(m,arg)=>m.Date" Context="arg">
    <input type="datetime" value="@arg.Tag.Getter(arg.Model,null)" />
</NTag>
<br />
Text中使用Markup：value:@@((DateTime)arg.Tag.Getter(arg.Model, null))
<br />
<label>
    <NTag Text="<span style='color:red;'>日期</span>" TextVisibility="NVisibility.Markup" Model="TestData" Getter="(m,a)=>m.Date" Context="arg">
        <input type="datetime" value="@((DateTime)arg.Tag.Getter(arg.Model,null))" />
    </NTag>
</label>
<br />
也可以直接使用childcontent：value:@@arg.Model.Date
<div>
    <NTag Model="TestData" Getter="(m,a)=>m.Date" Context="arg">
        <label> <span style='color:red;'>日期</span> <input type="datetime" value="@arg.Model.Date" /></label>
    </NTag>
</div>
getter 格式化:@@((m,a)=>m.Date.ToString("yyyy-MM-dd"))
<div>
    <NTag Model="TestData" Getter="@((m,a)=>m.Date.ToString("yyyy-MM-dd"))" Context="arg">
        <label> <span style='color:red;'>日期</span> <input type="datetime" value="@arg.Tag.Getter(arg.Model,null)" /></label>
    </NTag>
</div>
使用customAttributes ，借助外部方法推断TModel类型
<div>
    <NTag type="datetime"  Getter="@GetGetter(TestData,(m,a)=>m.Date)" Context="arg">
        <label> <span style='color:red;'>日期</span> <input @attributes="arg.Tag.CustomAttributes"  value="@arg.Tag.Getter(arg.Model,null)" /></label>
    </NTag>
</div>

@code {
    WeatherForecast TestData = new WeatherForecast { TemperatureC = 222, Date = DateTime.Now, Summary = "test summary" };

    Func<T, object, object> GetGetter<T>(T model, Func<T, object, object> func) {
        return (m, a) => func(model, a);
    }
}

```

 考察一下测试代码，我们发现 用作取值的 `arg.Tag.Getter(arg.Model,null)` 明显有些啰嗦了，调整一下RenderArgs,让它可以直接取值。
```
 public struct RenderArgs<TTag,TModel>   
    {
        public TTag Tag;
        public TModel Model;
        public object Arg;
        Func<TModel, object, object> _valueGetter;
        public object Value => _valueGetter?.Invoke(Model, Arg);
        public RenderArgs(TTag tag, TModel model, object arg  , Func<TModel, object, object> valueGetter=null) {
            this.Tag = tag;
            this.Model = model;
            this.Arg = arg;
            _valueGetter = valueGetter;
        }
    }
//NTag.razor.cs
 public RenderArgs<NTag<TModel>, TModel> Args(object arg = null)
        {

            return new RenderArgs<NTag<TModel>, TModel>(this, this.Model, arg,this.Getter);
        }
```



###  集合，Table 行列

集合的简单处理只需要循环一下。Test.razor
```
<ul>
    @foreach (var o in this.Datas)
    {
        <NTag Model="o" Getter="(m,a)=>m.Summary" Context="arg">
            <li @key="o">@arg.Value</li>
        </NTag>
    }
</ul>
@code {
  
    IEnumerable<WeatherForecast> Datas = Enumerable.Range(0, 10)
        .Select(i => new WeatherForecast { Summary = i + "" });
   
}
```
复杂一点的时候，比如Table，就需要使用列。
* 列有header：可以使用NTag.Text;
* 列要有单元格模板：NTag.ChildContent;
* 行就是所有列模板的呈现集合，行数据即是集合数据源的一项。
* 具体到table上，thead定义列，tbody生成行。

新增一个组件用于测试:TestTable.razor,试着用NTag呈现一个table。

```
<NTag TagId="table" TModel="WeatherForecast" Context="tbl">
    <table>
        <thead>
            <tr>
                <NTag Text="<th>#</th>"
                      TextVisibility="NVisibility.Markup"
                      ShowContent="false"
                      TModel="WeatherForecast"
                      Getter="(m, a) =>a"
                      Context="arg">
                    <td>@arg.Value</td>
                </NTag>
                <NTag Text="<th>Summary</th>"
                      TextVisibility="NVisibility.Markup"
                      ShowContent="false"
                      TModel="WeatherForecast"
                      Getter="(m, a) => m.Summary"
                      Context="arg">
                    <td>@arg.Value</td>
                </NTag>
                <NTag Text="<th>Date</th>"
                      TextVisibility="NVisibility.Markup"
                      ShowContent="false"
                      TModel="WeatherForecast"
                      Getter="(m, a) => m.Date"
                      Context="arg">
                    <td>@arg.Value</td>
                </NTag>
            </tr>
        </thead>
        <tbody>
            <CascadingValue Value="default(object)">
                @{ var cols = tbl.Tag.Children;
                    var i = 0;
                    tbl.Tag.ConsoleLog(cols.Count());
                }
                @foreach (var o in Source)
                {
                    <tr @key="o">
                        @foreach (var col in cols)
                        {
                            if (col is NTag<WeatherForecast> tag)
                            {
                                @tag.RenderContent(tag.Args(o,i ))
                            }
                        }
                    </tr>
                    i++;
                }
            </CascadingValue>
           
        </tbody>
    </table>
</NTag> 
 
@code {
    
    IEnumerable<WeatherForecast> Source = Enumerable.Range(0, 10)
        .Select(i => new WeatherForecast { Date=DateTime.Now,Summary=$"data_{i}", TemperatureC=i });

}
```

* 服务端模板处理时，代码会先于输出执行，直观的说，就是组件在执行时会有层级顺序。所以我们在tbody中增加了一个CascadingValue,推迟一下代码的执行时机。否则，`tbl.Tag.Children`会为空。
* thead中的NTag 作为列定义使用，与最外的NTag（table）正好形成父子关系。
* 观察下NTag，我们发现有些定义重复了，比如 TModel，单元格` <td>@arg.Value</td>`。下面试着简化一些。

之前测试Model呈现的代码中我们说到可以 “借助外部方法推断TModel类型”，当时使用了一个GetGetter方法，让我们试着在RenderArg中增加一个类似方法。

RenderArgs.cs:
```
public Func<TModel, object, object> GetGetter(Func<TModel, object, object> func) => func;
```
* GetGetter 极简单，不需要任何逻辑，直接返回参数。原理是 RenderArgs可用时，TModel必然是确定的。

用法：
```
<NTag Text="<th>#<th>"
                      TextVisibility="NVisibility.Markup"
                      ShowContent="false" 
                      Getter="(m, a) =>a"
                      Context="arg">
                    <td>@arg.Value</td>
```

作为列的NTag，每列的ChildContent其实是一样的，变化的只有RenderArgs，因此只需要定义一个就足够了。

NTag.razor.cs 增加一个方法,对于 ChildContent为null的组件我们使用一个默认组件来render。
```
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
```
 
TestTable.razor
```
<NTag TagId="table" TModel="WeatherForecast" Context="tbl">
    <table>
        <thead>
            <tr>
                <NTag Text="<th >#</th>"
                      TextVisibility="NVisibility.Markup"
                      ShowContent="false"
                      Getter="tbl.GetGetter((m,a)=>a)"
                      Context="arg">
                    <td>@arg.Value</td>
                </NTag>
                <NTag Text="<th>Summary</th>"
                      TextVisibility="NVisibility.Markup"
                      ShowContent="false"
                      Getter="tbl.GetGetter((m, a) => m.Summary)"/>
                <NTag Text="<th>Date</th>"
                      TextVisibility="NVisibility.Markup"
                      ShowContent="false"
                      Getter="tbl.GetGetter((m, a) => m.Date)"
                      />
            </tr>
        </thead>
        <tbody>
            <CascadingValue Value="default(object)">
                @{
                    var i = 0;
                    foreach (var o in Source)
                    {
                    <tr @key="o">
                        @tbl.Tag.RenderChildren(o, i++)
                    </tr>

                    }
                    }
            </CascadingValue>

        </tbody>
    </table>
</NTag>
```

### 结束
* 文中通过NTag演示一些组件开发常用技术，因此功能略多了些。
* TArgs 可以视作js组件中的option.
 





























 
 
    
 
       
        
 













