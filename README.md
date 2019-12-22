## Orleans.Storage.Elasticsearch

### 介绍

`Orleans.Storage.Elasticsearch` 是 Orleans Storage 扩展器，实现了自动存储、失败补偿、数据完整检查、自动创建索引等功能。

### 概念

> **Model**：是 `Orleans.Grain<TState>` 的状态，用于 `Orleans.Storage` 存储的存储对象，使用Orleans.Storage.Elasticsearch 存储时必须实现 `IElasticsearchModel` 接口。
>
>**ConcurrencyModel**：和 `Model` 一样是 `Orleans.Grain<TState>` 的状态，不同的是 ConcurrencyModel 是需要实现 `IElasticsearchConcurrencyModel` 接口。用于 Orleans.Storage.Elasticsearch 进行数据完整性检查。
>
>**Document**：`Elasticsearch` 存储的文档对象，用于创建索引和存储文档，建议使用 [Attribute mapping](https://www.elastic.co/guide/en/elasticsearch/client/net-api/6.x/attribute-mapping.html) 来标记字段类型。
>
> **Storage**：当数据补偿和完整性检查的时候，需要前往数据库查询数据更新到 Elasticsearch 中的时候使用。Storage 有两个需要实现的接口 `ICompensateCheckStorage<Model>` `ICompensateStorage<Model>`，前一个接口是用于补偿和完整性检查，后一个接口是用于数据补偿查询数据。

### 简单使用

 简单使用可以参考 [Github](https://github.com/lfzm/Orleans.Storage.Elasticsearch) 中的示例。

#### 第一步：先创建一个 Grain State，并且实现 `IElasticsearchModel` 接口

``` C#
[ElasticsearchType(Name = "user")]
public class UserModel : IElasticsearchModel
{
    public const string IndexName = "orleans.user";

    public int Id { get; set; }

    public string Name { get; set; }

    public string Sex { get; set; }

    public string GetPrimaryKey()
    {
        return this.Id.ToString();
    }
}
```

#### 第二步：创建一个 Grain 服务，并且实现 IUserGrain 接口

```C#
[StorageProvider(ProviderName = ElasticsearchStorage.DefaultName)]
public class UserGrain : Orleans.Grain<UserModel>, IUserGrain
{
    public Task AddUser(UserModel model)
    {
        this.State = model;
        return this.WriteStateAsync();
    }
}
```

#### 第三步：配置 ElasticsearchStorage 的 Elasticsearch 连接配置和Storage

```C#
var build = new SiloHostBuilder()
  .AddElasticsearchStorage(opt =>
  {
      opt.ConfigureConnection(new ConnectionSettings(new Uri("http://localhost:9200")));
      opt.AddStorage<UserModel>(UserModel.IndexName);
  }, ElasticsearchStorage.DefaultName)
```
>提示：
> 由于只使用 AddStorage\<UserModel\> 配置存储， `UserModel` 需要标记 [ElasticsearchType(Name = "user")] 把 `UserModel` 当做 `Document` 来使用。

### 配置

#### Elasticsearch 连接配置

`.ConfigureConnection(ConnectionSettings)`：配置 Elasticsearch 连接参数，可以前往参考[官方文档](https://www.elastic.co/guide/en/elasticsearch/client/net-api/6.x/connecting.html)。

#### 自动存储

自动存储只需要配置Grain State(下面简称Model)和 Elasticsearch Document 映射就可以在Orleans Grain直接调用 `this.WriteStateAsync()`，也可以使用 `ServiceProvider.GetElasticsearchStorage<AccountModel>()` 获取 Storage 进行存储、删除、查询等操作。

`.AddStorage<UserModel>(IndexName)`： 当 Model 和 Document 是一样的情况下，可以直接使用 Model 作为 Document ，和示例一样使用。

`.AddMapperStorage<AccountModel,AccountDocument>(IndexName)`：当 Model 和 Document 不一样的情况下使用，但是前提需要配置一个 Model 和 Document 转换器 `.AddDocumentConverter`。

`.AddDocumentConverter<TConverter>`：当配置了Model 和 Document时候需要配置一个转换器进行转换。

#### 失败补偿

失败补偿是当操作 Elasticsearch 存储、删除失败的时候，需要尝试重新操作，失败补偿是基于 Orleans Remindable 实现的。所以需要配置 Orleans Remindable：

```C#
.UseAdoNetReminderService(opt =>
{
    opt.ConnectionString = "连接字符串";
    opt.Invariant = "MySql.Data.MySqlClient";
})
```

由于补偿插入或者修改的时候需要最新的数据，所以需要提供一个查询最新数据的功能，Orleans.Storage.Elasticsearch 提供了一个查询最新数据的接口`ICompensateStorage<Model>` 。

需要实现这个接口(CompensateStorage)然后在进行配置：

`.AddStorage<UserModel,CompensateStorage>(IndexName)` 
`.AddMapperStorage<AccountModel,AccountDocument,CompensateStorage>(IndexName)`

Grain\<State\> 激活的时候默认是前往 Elasticsearch 获取数据，但是有时候需要前往数据库获取数据来进行激活，只需要实现 `ICompensateStorage<Model>` 接口之后并且进行配置 `GrainStorage.ConfigureToDBRead(typeof(AccountModel),typeof(UserModel))` 即可。

#### 完整检查

当补偿失败或者想定时把数据同步到 Elasticsearch 中，数据完整检查提供了定时检查和间隔检查两种方式。

数据完整检查原理：是通过数据库未同步的标记和版本号 与 Elasticsearch 中的数据进行对比，如果没有同步在根据数据唯一标识前往数据库查询数据存储到 Elasticsearch 中。

>提示：完整性检查之后补偿的数据失败还会再次启动补偿。

启动数据完整性要求：

1、需要在需要完整检查的表中增加 `IsSync(是否同步)` 、`VersionNo(数据版本号)` 这个两个字段。

2、`Model` 需要实现 `IElasticsearchConcurrencyModel` 接口。

3、 `Storage` 需要实现 `ICompensateCheckStorage<TModel>` 这个接口。

只要完成上面的要求、配置和失败补偿配置一样，就自动启动完整性检查，默认 `每天晚上凌晨` 启动数据完整检查并且补偿。

##### 定时检查

通过配置下一次检查的时间和检查结果来启动定时检查

`.AddStorage<UserModel,CompensateStorage>(IndexName,(DateTime)checkStartTime,(TimeSpan)checkInterval)` `.AddMapperStorage<AccountModel,AccountDocument,CompensateStorage>(IndexName,(DateTime)checkStartTime,(TimeSpan)checkInterval)`


##### 间隔检查

通过配置检查时间间隔来启动间隔检查：

`.AddStorage<UserModel,CompensateStorage>(IndexName,(TimeSpan)checkInterval)` `.AddMapperStorage<AccountModel,AccountDocument,CompensateStorage>(IndexName,(TimeSpan)checkInterval)`