# Introduction
Trellis provides easy lazy loading capabilites for a database-like store. It is designed to work mainly with NoSQL databases. Assumptions about the database include:

* It stores objects consisting of named fields with various values
* It stores objects in named "collections"
* It supports interacting with selected from an object and not others
* It has support for array fields

If the database fulfills those requirements, an `IDB` interface implementation can be written for it and Trellis can be used.

# LazyModels
Models are the representation of an object in a collection in DB. 

## Configuration

### The model class
Database model classes need to inherit from `LazyModel`. Model fields are set up as properties. In the getter and setter methods, use `PropertyGetter()` and `PropertySetter()` inherited from `LazyModel`. Due to limitations of the language, this has to be done manually (may be later converted to PostSharp). The name of the field that you pass to property methods will be used as field name for the database adapter.

The model also needs a public constructor that will provide objects necessary for model creation.

Example model:
```csharp
public class UserAccountModel :LazyModel
{
    public string Username
    {
        get { return PropertyGetter("Username"); }
        set { PropertySetter("Username", value); }
    }
    public DateTime CreatedAt
    {
        get { return PropertyGetter("CreatedAt"); }
        set { PropertySetter("CreatedAt", value); }
    }
    
    public UserAccountModel(Id id, IDBCollection collection)
            :base(id, collection)
    { }
}
```

### The ModelProvider
Trellis uses a `ModelProvider` class that builds on the `IDB` interface. It is used to query the database and retrieve appropriate models or to create new ones. Its constructor
```
public ModelProvider(IDB db, IDictionary<Type, string> collectionNameDict)
```
allows passing a dictionary defining collection names for different types of models. If there is no collection name defined for a model, a default, straightforward one will be used ("UserModels" for type `UserModel`, for example).

## Usage
Having the configuration work done, using the models is super easy. Creation of `ModelProvider`s is best done with an IoC container. It is preferable to instatiate per-model generic versions of `ModelProvider` to maintain type safety.
```csharp
var collectionNameConfig = new Dictionary<Type, string>
{
    {typeof(UserAccountModel), "Accounts"}
};
var provider = new ModelProvider(yourDatabaseAdapter, collectionNameConfig);
var accountProvider = new ModelProvider<UserAccountModel>(provider);
```

### Getting data from a model
```csharp
var account = accountProvider.Get(0);
var username = account.Username;
```
The `Get()` call itself does not perform any interaction with the database. Only when getting the Username field, an appropriate request will be sent to retrieve the value. This prevents loading unnecessary values from the model.

#### Preload
When you need to use a lot of fields from a model, the default behavior of one-dbcall-per-field becomes undesired. The `Preload()` method allows you to preload values in one batch dbcall before using them.
```csharp
var account = accountProvider.Get(0);

// DB call here
account.Preload(x => x.Username,
                x => x.CreatedAt);
                
// No further DB calls
var username = account.Username;
var createdAt = account.CreatedAt;
```

### Setting model fields
```csharp
var account = accountProvider.Get(0);
account.Username = "banana";
account.CreatedAt = new DateTime(2001, 1, 1);
// DB call at this point
account.Commit();
```
Setting model fields does not interact with the DB. It instead journals your changes. Then you use the `Commit()` method to send one optimized DB write for the model.

# Aggregators
Aggregators consist of several models and consolidate information from them into a single entity. They usually represent domain entities. 

## Configuration

### The aggregator class
The aggregator class setup is similar to model setup. All aggregators inherit from LazyAggregator. One additional thing that you need to do is to setup a mapping from models to the aggregator. It is done through an AutoMapper-like fluent API. 

The simplest use case is declaring only model types that the aggregator is using and Trellis will automatically map all properties with corresponding names and types from models to the aggregator. In more advanced cases, an explicit mapping definition is required.

The models that the aggregator is using all have to have the same ID. If an ID is different, it means the model requires a separate aggregator as it is a different entity.

In the constructor, you pass instances of the models that the aggregator is using.

Assuming the following model definitions (getter and setter implementations omitted for clarity):
```csharp
class PaymentDate : LazyModel
{
    public int DayPaid { ... }
    public int MonthPaid { ... }
    public int YearPaid { ... }
    public PaymentDate(Id id, IDBCollection collection)
        : base(id, collection)
    {}
}
```
```csharp
class PaymentRecord : LazyModel
{
    public int Amount { ... }
    public string ProductName { ... }
    public PaymentRecord(Id id, IDBCollection collection)
        : base(id, collection)
    {}
}
```
we can have the following aggregator setup:
```csharp
class Payment : LazyAggregator
{
    public int PaymentAmount
    {
        get { return PropertyGetter<int>("PaymentAmount"); }
        set { PropertySetter("PaymentAmount", value); }
    }
    public string ProductName
    {
        get { return PropertyGetter<string>("ProductName"); }
        set { PropertySetter("ProductName"), value); }
    }
    public DateTime Date
    {
        get { return PropertyGetter<DateTime>("Date"); }
        set { PropertySetter("Date", value); }
    }
    public Payment(
        IAggregatorProvider provider,
        PaymentRecord record,
        PaymentDate date)
        : base(provider, record, date)
    {}
    static Payment()
    {
        Using<Payment, PaymentRecord>();
        Using<Payment, PaymentDate>();
        
        Setup<Payment>()
            .Field(x => x.PaymentAmount)
                .OneToOne<PaymentRecord>(x => x.Amount)
            .Field(x => x.Date)
                .From(a => new DateTime(a.M<PaymentDate>().Year,
                                        a.M<PaymentDate>().Month,
                                        a.M<PaymentDate>().Day))
                .To<PaymentDate>(x => x.DayPaid).With(dt => dt.Day)
                .To<PaymentDate>(x => x.MonthPaid).With(dt => dt.Month)
                .To<PaymentDate>(x => x.YearPaid).With(dt => dt.Year)
                .Using<PaymentDate>(x => x.DayPaid,
                                    x => x.MonthPaid,
                                    x => x.YearPaid);
    }
}
```
It's convenient to put the mapping configuration in the static constructor of the class, although it can be done somewhere else as long as it's before any aggregator operations. We will go through it step by step.

#### *Using* declarations 
They are in the form `LazyAggregator.Using<AggregatorType, ModelType>()` and declare that the aggregator of type `AggregatorType` is using model `ModelType`. They are needed for the automapping functionality to work. Model types in *Using* declarations should be consistent with aggregator constructor agrument types.

#### Field mapping
* Field `ProductName` has a corresponding field in the models with the same name and type. Therefore no explicit mapping is needed.
* Field `PaymentAmount` maps to field `PaymentRecord.Amount` which is the same thing, but with a different name. We can setup a simple one-to-one mapping with `OneToOne<ModelType>()`.
* Field `Date` is more complicated because it uses several model fields to build a single aggregator value. We have to use a full explicit mapping configuration.
    - `From()` is used to define a fuction that transforms model field values to the aggregator field value. To get models, you can use the `M<ModelType>()` method available on the function's argument.
    - `To<ModelType>()` and `With()` are used to define a function that transforms the aggregator field value to a model field value. If there are several model fields, each field needs its separate configuration. First you select the target model field with `To<ModelType>()`, then define the transform function using `With()`.
    - `Using<ModelType>()` defines all model fields used by the aggregator field. It allows for preloading capabilities. I'm not sure if it's necessary because the information can be inferred from `To<ModelType>()` configs, but it'll stay for now. If a field uses several models, several `Using()` configs are needed.

## Usage
All rules of using models apply to aggregators, including`Commit()` and `Preload()` (here called `PreloadAgg()` because of reasons). Like in models, you instantiate the `AggregatorProvider` and generic variations of it.

## Nested aggregators
Nesting aggregators is supported, therefore providing SQL JOIN-like functionality. If a model contains a field with ID of another model, this situation can be mapped to nested aggregators. For example (setter and getter implementations omitted):
```csharp
class ItemModel : LazyModel
{
    public string Description { ... }
    public ItemModel(Id id, IDBCollection collection)
        : base(id, collection)
    {}
}
```
```csharp
class ItemListingModel : LazyModel
{
    public string Title { ... }
    public Id ItemId { ... }
    public ItemListingModel(Id id, IDBCollection collection)
        : base(id, collection)
    {}
}
```
```csharp
class Item : LazyAggregator
{
    public string Description { ... }
    public Item(IAggregatorProvider provider, ItemModel item)
        : base(provider, item)
    static Item()
    {
        Using<Item, ItemModel>();
    }
}
```
```csharp
class ItemListing : LazyAggregator
{
    public string Title { ... }
    public Item Item { ... }
    public ItemListing(
        IAggregatorProvider provider,
        ItemListingModel listing)
        : base(provider, listing)
    {}
    static ItemListing()
    {
        Using<ItemListing, ItemListingModel>();
        Setup<ItemListing>()
            .ForeignAggregator(x => x.Item)
                .IdFrom<ItemListingModel>(x => x.ItemId);
    }
}
```
Here, the `ItemListingModel` contains an `Id` of another model, `ItemModel`. In the aggregators, the field is mapped by selecting the foreign aggregator field and then providing information where to find the aggregator's `Id`.

And it works:
```csharp
var description = itemListing.Item.Description
```
will retrieve the description by first retrieving the `Id` of the foreign aggregator from the first one, and then retrieving the Description from that.

Using `Commit()' on an aggregator also commits any changes made in its nested aggregators. Specifying the nested aggregator field in `Preload()` will preload the whole nested aggregator (and its nested ones, recursively).

# Array support //TODO
Models often contain array fields. Treating them as whole values is often impractical because they tend to get quite big. To load arrays lazily, declare them as `LazyList<T>`. This gives you:

* Adding (appending) items lazily with the `Append()` method
* Removing items lazily with the `Remove()` method
* Lazy loading and setting elements by index
* Querying for size without retrieving the array with `Count()`

It is also possible to make arrays of aggregators by specifying a `LazyList<Id>` field in a model. It is mapped to aggregator list using a special config method (TODO).

# Best practices
Trellis enables composing functions that operate on models and aggregators without worrying about loading the data from database and explicit database calls that obfuscate application logic. A common use would be:
```csharp
aggregator.Preload(<some fields>);

// operate on aggregator
SomeTransformation(aggregator);
SomeOperation(aggregator, something);
AnotherTransformation(aggregator);

aggregator.Commit();
```
The `Preload()` call is never mandatory and the code will work without worrying about which fields exactly are used by the transformations. At the same time, it's easy to speed up the code with `Preload()`.

# TODOs
* Make Trellis all-async to make creating MongoDB adapter possible
* Finish Array support and loading Arrays of aggregators
* Fix bugs