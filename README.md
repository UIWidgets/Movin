# Movin

这是一个UIWidgets组件，用来显示Lottie格式的动画。

其中Lottie格式的解析代码来自于U.Movin。

这是一个未完成的工作，代码目前无法使用。

## TODO

### 理解U.Movin代码

U.Movin是一个C#版本的Lottie解析库，将Lottie格式的JSON文件解析，并通过Unity中的GameObject将动画效果展示出来。
具体而言，U.Movin将Lottie中的层级结构翻译成GameObject层级，并将Lottie中需要绘制的图形转换为`MeshRenderer`和`MeshFilter`组件挂载到GameObject上。

#### Lottie解析

所有Lottie文件解析的逻辑都写在`BodymovinContent.cs`文件中，确切地说是`BodymovinContent`类中。

首先解析JSON，JSON库用的是SimpleJSON，在工程中包含完整源码。

解析Lottie文件的结果是得到一个`BodymovinContent`对象。BodyMovin中的数据类型结构如下图所示(图中三叉代替箭头表示包含一个数组)

![](https://github.com/UIWidgets/Movin/blob/master/U.Movin-Bodymovin-Classes.png?raw=true)

一个`BodymovinContent`中，除了一些基本参数外，就是一个`BodymovinLayer`数组。一个`BodymovinLayer`又包含一个`BodymovinShape`数组，`BodymovinShape`包含`BodymovinShapeItem`数组和`BodymovinShapePath`数组。这其中每一个层级都包含`BodymovinAnimatedProperties`，即描述动画的数据。

#### 渲染

U.Movin是通过把上述的每一个类型对象转换成一个GameObject，借助Unity对GameObject的渲染流程完成渲染的。如果某个对象(Layer、Shape或ShapeItem等)中包含可以直接渲染的内容，就在对应的GameObject上挂载上`MeshFilter`和`MeshRenderer`组件。

针对上述层级中的每一层，U.Movin也定义了一个类型来进行Bodymovin类型到Unity游戏对象之间的转换。如下图所示，每一个类型都包含一个`content`成员，用来保存一个对应这一层的数据。

![](https://github.com/UIWidgets/Movin/blob/master/U.Movin-Movin-Classes.png?raw=true)

每个对象也会包含一个GameObject的引用，以及对这个GameObject的Transform组件的引用。

所有Layers组成树的结构。在Lottie文件中，这个树是通过在每个Layer中指定其父Layer的索引来存储的。将Lottie文件解析为`Movin`之后，各Layer之间的父子关系被转移到它们的Transform上。下面的代码片段展示了根据存储在`BodymovinLayer`中的Layer父子关系，设置各Layer的Transform的过程。

```C#
for (int i = 0; i < layers.Length; i++) {
  MovinLayer layer = layers[i];
  int p = layer.content.parent;
  if (p <= 0) { continue; }
  layer.transform.SetParent(layersByIndex[p].content.shapes.Length > 0
                            ? layersByIndex[p].transform.GetChild(0)
                            : layersByIndex[p].transform, false);
}
```

注意到，当把一个Layer设置为另一个Layer的儿子时，如果父Layer的`shapes`不为空的话，要把子Layer挂到父Layer的`shapes[0]`下面，而不是直接挂在父Layer下面。

下图是一个示例，表示Lottie的解析结束后，各`MovinLayer`和`MovinShape`的Transform之间形成的树结构的样子。

![](https://github.com/UIWidgets/Movin/blob/master/U.Movin-Layer-Tree.png?raw=true)

#### 动画

U.Movin动画是用时间驱动的，指定一个时间点，调用`Update()`函数，Movin就会把整个GameObject树调整到指定时刻的状态，更新Transform的值和所有Mesh。

Lottie文件中定义了frameRate和totalFrames参数。Movin可以根据这些信息计算出当前时间对应的帧数，并调用每个Layer的`Update()`函数。Layer会调整自己的Transform，并调用每个Shape的`Update()`函数。Shape会调用`UpdateMesh()`函数。

### 需要修改的部分

上文提到的树形结构，U.Movin是直接使用UnityEngine自带的Transform构建的。如果直接沿用这个逻辑，可以考虑丢弃GameObject，只保留Transform，不过仍然可能会比较笨重。可以考虑实现一个轻量级的只包含变换矩阵的数据结构。

最后负责产生图形的是`MovinShape`，逻辑实现在`UpdateMesh()`函数里。U.Movin通过调用`Unity.VectorGraphics.VectorUtils.FillMesh()`函数生成Mesh。这一部分逻辑是我们要换成使用UIWidgets的自定义RenderObject来实现的。
