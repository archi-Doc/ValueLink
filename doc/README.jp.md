## CrossLink
![Nuget](https://img.shields.io/nuget/v/CrossLink) ![Build and Test](https://github.com/archi-Doc/CrossLink/workflows/Build%20and%20Test/badge.svg)

ソースジェネレーターと [Arc.Collection](https://github.com/archi-Doc/Arc.Collection) を使用したC#ライブラリです。

オブジェクト間に複数のリンクを張って、柔軟に管理したり検索したり出来ます。

よく分からない？

オブジェクト`T` に対して、カスタム`List<T>` を作成します。しかも、普通のジェネリックコレクションより柔軟で拡張性があり、なおかつ高速です。

一言で言えば、速くて便利！オブジェクトを扱うプログラムでは必須です！

ええ、こんな説明じゃ分からないでしょう。

下の[サンプルコード](#quick-start)をみてください。



## Table of Contents

- [Quick Start](#quick-start)
- [Performance](#performance)
- [How it works](#how-it-works)
- [Chains](#chains)
- [Features](#features)
  - [Serialization](#serialization)
  - [AutoNotify](#autonotify)
  - [AutoLink](#autolink)
  - [ObservableCollection](#observablecollection)



## Quick Start

ソースジェネレーターなので、ターゲットフレームワークは .NET 5 以降です。

まずはPackage Manager Consoleでインストール。

```
Install-Package CrossLink
```

サンプルコードです。

```csharp
using System;
using System.Collections.Generic;
using CrossLink;

#pragma warning disable SA1300

namespace ConsoleApp1
{
    [CrossLinkObject] // 対象のクラスに CrossLinkObject属性を追加します
    public partial class TestClass // ソースジェネレーターでコード追加するので、partial classが必須
    {
        [Link(Type = ChainType.Ordered)] // 対象のメンバーにLink属性を追加します。TypeにChainType（Collectionの種類のようなもの）を指定します。
        private int id; // 対象となるメンバー。これを元に、プロパティ Id と IdLink が追加されます。
        // プロパティ Id を使用して、値の取得・更新（値、リンク）を行います。
        // プロパティ IdLink はオブジェクト間の情報を保存します。CollectionのNodeのようなものです。

        [Link(Type = ChainType.Ordered)] // ChainType.Ordered はソート済みコレクション。SortedDictionary と考えていただけば
        public string name { get; private set; } = string.Empty; // プロパティ Name と NameLink が追加

        [Link(Type = ChainType.Ordered)]// 同上
        private int age; // プロパティ Age と AgeLink が追加

        [Link(Type = ChainType.StackList, Name = "Stack")] // Nameで名称を指定して、StackListを追加。コンストラクターには複数のLinkを付加出来ます。
        [Link(Type = ChainType.List, Name = "List")] // Listを追加
        public TestClass(int id, string name, int age)
        {
            this.id = id;
            this.name = name;
            this.age = age;
        }

        public override string ToString() => $"ID:{this.id,2}, {this.name,-5}, {this.age,2}";
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("CrossLink Quick Start.");
            Console.WriteLine();

            var g = new TestClass.GoshujinClass(); // まずは、オブジェクト管理のクラス Goshujin を作成
            new TestClass(1, "Hoge", 27).Goshujin = g; // TestClassを作成し、Goshujinを設定します（Goshujin側にもTestClassが登録されます）
            new TestClass(2, "Fuga", 15).Goshujin = g;
            new TestClass(1, "A", 7).Goshujin = g;
            new TestClass(0, "Zero", 50).Goshujin = g;

            ConsoleWriteIEnumerable("[List]", g.ListChain); // ListChain（コンストラクタにLinkが追加されたやつ）は実質的に List<TestClass> です
            /* Result;  作成順に並びます
                 ID: 1, Hoge , 27
                 ID: 2, Fuga , 15
                 ID: 1, A    ,  7
                 ID: 0, Zero , 50 */

            Console.WriteLine("ListChain[2] : "); // インデックスアクセスが可能
            Console.WriteLine(g.ListChain[2]); // ID: 1, A    ,  7
            Console.WriteLine();

            ConsoleWriteIEnumerable("[Sorted by Id]", g.IdChain);
            /* IdChain は ChainType.Ordered なので、ソート済み
                 ID: 0, Zero , 50
                 ID: 1, Hoge , 27
                 ID: 1, A    ,  7
                 ID: 2, Fuga , 15 */

            ConsoleWriteIEnumerable("[Sorted by Name]", g.NameChain);
            /* 同様にNameでソート済み
                 ID: 1, A    ,  7
                 ID: 2, Fuga , 15
                 ID: 1, Hoge , 27
                 ID: 0, Zero , 50 */

            ConsoleWriteIEnumerable("[Sorted by Age]", g.AgeChain);
            /* 同様にAgeでソート済み
                 ID: 1, A    ,  7
                 ID: 2, Fuga , 15
                 ID: 1, Hoge , 27
                 ID: 0, Zero , 50 */

            var t = g.ListChain[1];
            Console.WriteLine($"{t.Name} age {t.Age} => 95");
            t.Age = 95; // Fugaの年齢を95にすると、
            ConsoleWriteIEnumerable("[Sorted by Age]", g.AgeChain);
            /* なんと AgeChain が更新されています！
                 ID: 1, A    ,  7
                 ID: 1, Hoge , 27
                 ID: 0, Zero , 50
                 ID: 2, Fuga , 95 */

            ConsoleWriteIEnumerable("[Stack]", g.StackChain);
            /* こちらは Stack
                 ID: 1, Hoge , 27
                 ID: 2, Fuga , 95
                 ID: 1, A    ,  7
                 ID: 0, Zero , 50 */

            t = g.StackChain.Pop(); // Stackの先頭のオブジェクトを取得し、Stackから削除します。影響するのはStackChainだけなのでご注意ください。
            Console.WriteLine($"{t.Name} => Pop");
            t.Goshujin = null; // 他のChainから削除するには、Goshujinをnullにします。
            Console.WriteLine();

            ConsoleWriteIEnumerable("[Stack]", g.StackChain);
            /* Zero が解放されました・・・
                 ID: 1, Hoge , 27
                 ID: 2, Fuga , 95
                 ID: 1, A    ,  7 */

            var g2 = new TestClass.GoshujinClass(); // Goshujin2 を作成
            t = g.ListChain[0];
            Console.WriteLine($"{t.Name} Goshujin => Goshujin2");
            Console.WriteLine();
            t.Goshujin = g2; // Goshujin から Goshujin2 に変更すると
            ConsoleWriteIEnumerable("[Goshujin]", g.ListChain);
            ConsoleWriteIEnumerable("[Goshujin2]", g2.ListChain);
            /*  各種Chainが更新されます
             *  [Goshujin]
                 ID: 2, Fuga , 95
                 ID: 1, A    ,  7
                [Goshujin2]
                 ID: 1, Hoge , 27*/

            // g.IdChain.Remove(t); // t は Goshujin2 の所有物なので、これはエラー
            // t.Goshujin.IdChain.Remove(t); // こちらはOK（t.GosjujinはGoshujin2）
            
            Console.WriteLine("[IdChain First/Next]");
            t = g.IdChain.First; // Link interface を使って、オブジェクトを列挙します
            while (t != null)
            {
                Console.WriteLine(t);
                t = t.IdLink.Next; // Next は Link ではなく、Objectそのものなのでご注意ください
            }

            static void ConsoleWriteIEnumerable<T>(string? header, IEnumerable<T> e)
            {// オブジェクトを画面に出力
                if (header != null)
                {
                    Console.WriteLine(header);
                }

                foreach (var x in e)
                {
                    Console.WriteLine(x!.ToString());
                }

                Console.WriteLine();
            }
        }
    }
}
```



## Performance

パフォーマンスは最優先事項です。

CrossLinkは、ジェネリックコレクションより込み入った処置を行っていますが、実際はジェネリックコレクションより高速に動作します（主に[Arc.Collection](https://github.com/archi-Doc/Arc.Collection)のおかげです）。

`SortedDictionary<TKey, TValue>` と比べてみましょう。

`H2HClass` という簡単なクラスを作成します。

```csharp
[CrossLinkObject]
public partial class H2HClass2
{
    public H2HClass2(int id)
    {
        this.id = id;
    }

    [Link(Type = ChainType.Ordered)]
    private int id;
}
```

ジェネリック版。クラスを作成し、コレクションに追加していきます。

```csharp
var g = new SortedDictionary<int, H2HClass>();
foreach (var x in this.IntArray)
{
    g.Add(x, new H2HClass(x));
}
```

こちらはCrossLink版。同じような処理をしています。

```csharp
var g = new H2HClass2.GoshujinClass();
foreach (var x in this.IntArray)
{
    new H2HClass2(x).Goshujin = g;
}
```

こちらが結果。 なんと`SortedDictionary<TKey, TValue>` より高速です。

| Method                     | Length |       Mean |    Error |   StdDev |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
| -------------------------- | ------ | ---------: | -------: | -------: | -----: | -----: | ----: | --------: |
| NewAndAdd_SortedDictionary | 100    | 7,209.8 ns | 53.98 ns | 77.42 ns | 1.9379 |      - |     - |    8112 B |
| NewAndAdd_CrossLink        | 100    | 4,942.6 ns | 12.28 ns | 17.99 ns | 2.7084 | 0.0076 |     - |   11328 B |

`Id` を変更すると、当然コレクションの更新（値の削除・追加）が必要です。

CrossLinkは断然高速で、`SortedDictionary` の約3倍のパフォーマンスです（CrossLinkは内部でNode管理をしているので、当然と言えば当然ですが）。

| Method                        | Length |       Mean |    Error |   StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
| ----------------------------- | ------ | ---------: | -------: | -------: | -----: | ----: | ----: | --------: |
| RemoveAndAdd_SortedDictionary | 100    | 1,491.1 ns | 13.01 ns | 18.24 ns | 0.1335 |     - |     - |     560 B |
| RemoveAndAdd_CrossLink        | 100    |   524.1 ns |  3.76 ns |  5.63 ns | 0.1717 |     - |     - |     720 B |



## How it works

CrossLinkは既存のクラスに、Goshujinという内部クラスと、いくつかのプロパティを追加することで動作します。

実際には、

1. `GoshujinClass` という内部クラスを追加
2. `Goshujin` プロパティを追加
3. `Link` 属性が付加されたメンバーに対応するプロパティを追加します。プロパティ名は、メンバー名の頭文字が大文字に変換されたものです（`id` なら `Id` になる）。
4. `Link` 属性が付加されたメンバーに対応する`Link` フィールドを追加します。こちらの名称は、プロパティ名にLinkがついたものになります（`Id` なら `IdLink` になる）。

という流れです。



用語

- ```Object```: 情報を保持する、一般的なオブジェクト。
- ```Goshujin```: オブジェクトのオーナークラス。このクラスを介して、オブジェクトの管理・操作を行います。
- ```Chain```: コレクションのようなもの。`Goshujin` は複数の `Chain` を保持し、オブジェクトを様々な形式で管理できます。
- ```Link```: コレクションにおけるNodeのようなもの。オブジェクトは内部に複数の`Link`を持ち、オブジェクト間の情報を保持します。



実際に、ソースジェネレーターでどのようなコードが生成され、どのようにCrossLinkが動作するのか見てみましょう。

```csharp
public partial class TinyClass // partial class が必須
{
    [Link(Type = ChainType.Ordered)] // Link属性を追加
    private int id;
}
```

プロジェクトをビルドすると、CrossLinkはまず `GoshujinClass`という内部クラスを作成します。`GoshujinClass` は `TinyClass` を操作・管理するクラスです。

```csharp
public sealed class GoshujinClass : IGoshujin
{// ご主人様は、日本語で Goshujin-sama という意味です
    
    public GoshujinClass()
    {
        // IdChainはTinyClassのソート済みコレクションです
        this.IdChain = new(this, static x => x.__gen_cl_identifier__001, static x => ref x.IdLink);
    }

    public OrderedChain<int, TinyClass> IdChain { get; }
}
```

次のコードでは `Goshujin` インスタンス/プロパティを追加します。

```csharp
private GoshujinClass? __gen_cl_identifier__001; // 実際の Goshujinインスタンス

public GoshujinClass? Goshujin
{
    get => this.__gen_cl_identifier__001;
    set
    {// Goshujinインスタンスをセットします
        if (value != this.__gen_cl_identifier__001)
        {
            if (this.__gen_cl_identifier__001 != null)
            {// TinyClassを以前のGoshujinから解放します
                this.__gen_cl_identifier__001.IdChain.Remove(this);
            }

            this.__gen_cl_identifier__001 = value;// インスタンスを設定します
            if (value != null)
            {// 新しいGoshujinにお仕えします
                value.IdChain.Add(this.id, this);
            }
        }
    }
}
```

最後に、メンバーに対応する `Link` と プロパティを追加します。

inally, CrossLink adds a link and a property which is used to modify the collection and change the value.

```csharp
public OrderedChain<int, TinyClass>.Link IdLink; // Link is like a Node.

public int Id
{// プロパティ "Id" は、メンバー "id" から作成されました
    get => this.id;
    set
    {
        if (value != this.id)
        {
            this.id = value;
            // 値が更新されると、IdChainも更新されます
            this.Goshujin.IdChain.Add(this.id, this);
        }
    }
}
```



## Chains

Chainはオブジェクトのコレクションクラスのようなもので、CrossLinkでは以下のChainを実装しています。

| Name                  | Structure   | Access | Add      | Remove   | Search   | Sort       | Enum.    |
| --------------------- | ----------- | ------ | -------- | -------- | -------- | ---------- | -------- |
| ```ListChain```       | Array       | Index  | O(1)     | O(n)     | O(n)     | O(n log n) | O(1)     |
| ```LinkedListChain``` | Linked list | Node   | O(1)     | O(1)     | O(n)     | O(n log n) | O(1)     |
| ```QueueListChain```  | Linked list | Node   | O(1)     | O(1)     | O(n)     | O(n log n) | O(1)     |
| ```StackListChain```  | Linked list | Node   | O(1)     | O(1)     | O(n)     | O(n log n) | O(1)     |
| `OrderedChain`        | RB Tree     | Node   | O(log n) | O(log n) | O(log n) | Sorted     | O(log n) |
| `ReverseOrderedChain` | RB Tree     | Node   | O(log n) | O(log n) | O(log n) | Sorted     | O(log n) |
| ```UnorderedChain```  | Hash table  | Node   | O(1)     | O(1)     | O(1)     | -          | O(1)     |
| ```ObservableChain``` | Array       | Index  | O(1)     | O(n)     | O(n)     | O(n log n) | O(1)     |

こーゆーChainが欲しい的な要望ありましたらご連絡ください。



## Features

### Serialization

複雑にリンクされたオブジェクトのシリアライズは結構面倒です。

しかし、[Tinyhand](https://github.com/archi-Doc/Tinyhand) との合わせ技で簡単にシリアライズできます！

やり方は簡単。`Tinyhand` パッケージをインストールして、`TinyhandObject` 属性を追加して、`Key` 属性を各メンバーに追加するだけです！

```
Install-Package Tinyhand
```

```csharp
[CrossLinkObject]
[TinyhandObject] // TinyhandObject属性を追加
public partial class SerializeClass // partial class を忘れずに
{
    [Link(Type = ChainType.Ordered, Primary = true)] // Primary Link（すべてのオブジェクトが登録されるLink）を指定すると、さらにシリアライズのパフォーマンスが改善します
    [Key(0)] // Key属性（シリアライズの識別子。stringかint）を追加
    private int id;

    [Link(Type = ChainType.Ordered)]
    [Key(1)]
    private string name = default!;

    public SerializeClass()
    {// Tinyhandのため、デフォルトコンストラクタ（引数のないコンストラクタ）が必要です
    }

    public SerializeClass(int id, string name)
    {
        this.id = id;
        this.name = name;
    }
}
```

テストコード：

```csharp
var g = new SerializeClass.GoshujinClass(); // Goshujinを作成
new SerializeClass(1, "Hoge").Goshujin = g; // オブジェクト追加
new SerializeClass(2, "Fuga").Goshujin = g;

var st = TinyhandSerializer.SerializeToString(g); // これだけでシリアライズ出来ます！
var g2 = TinyhandSerializer.Deserialize<SerializeClass.GoshujinClass>(TinyhandSerializer.Serialize(g)); // バイナリにシリアライズして、それをデシリアライズします。簡単でしょう？
```



### AutoNotify

`Link` 属性の `AutoNotify`プロパティを `true` にすると、CrossLinkは `INotifyPropertyChanged` を自動で実装します。

```csharp
[CrossLinkObject]
public partial class AutoNotifyClass
{
    [Link(AutoNotify = true)] // AutoNotifyをtrueに
    private int id;

    public void Reset()
    {
        this.SetProperty(ref this.id, 0); // SetPropertyを呼ぶと、手動で値の更新とPropertyChanged の呼び出しが出来ます。
    }
}
```

テストコード：

```csharp
var c = new AutoNotifyClass();
c.PropertyChanged += (s, e) => { Console.WriteLine($"Id changed: {((AutoNotifyClass)s!).Id}"); };
c.Id = 1; // 値を変更すると、自動的に PropertyChange が呼ばれます。
c.Reset(); // 手動で
```

自動生成コード：

```csharp
public partial class AutoNotifyClass : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }
        
        storage = value;
        this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        return true;
    }

    public int Id
    {
        get => this.id;
        set
        {
            if (value != this.id)
            {
                this.id = value;
                this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs("Id"));
            }
        }
    }
}
```



### AutoLink

デフォルトの動作では、オブジェクトのGoshujinが設定されると自動でオブジェクトをリンク（GoshujinのChainに登録する）します。

自動でリンクしたくない場合は、`AutoLink` プロパティを `false` に設定してください。

 ```csharp
[CrossLinkObject]
public partial class ManualLinkClass
{
    [Link(Type = ChainType.Ordered, AutoLink = false)] // AutoLinkをfalse
    private int id;

    public ManualLinkClass(int id)
    {
        this.id = id;
    }

    public static void Test()
    {
        var g = new ManualLinkClass.GoshujinClass();

        var c = new ManualLinkClass(1);
        c.Goshujin = g; // 自動でリンクされません
        Debug.Assert(g.IdChain.Count == 0, "Chain is empty.");

        g.IdChain.Add(c.id, c); // 手動でリンクします
        Debug.Assert(g.IdChain.Count == 1, "Object is linked.");
    }
}
 ```



### ObservableCollection

MVVM？バインディング？

面倒なことばかりでしょう。

`ObservableChain` を使うと、簡単にバインディングできます。

コンストラクタに `[Link(Type = ChainType.Observable, Name = "Observable")]` を追加するだけです。

```csharp
[CrossLinkObject]
public partial class ObservableClass
{
    [Link(Type = ChainType.Ordered, AutoNotify = true)] // もちAutoNotify
    private int id { get; set; }

    [Link(Type = ChainType.Observable, Name = "Observable")]
    public ObservableClass(int id)
    {
        this.id = id;
    }
}
```

テストコード：

```csharp
var g = new ObservableClass.GoshujinClass();
ListView.ItemSource = g.ObservableChain;// ObservableChainをObservableCollectionのように使用できます
new ObservableClass(1).Goshujin = g;// これでListViewが更新！
```

