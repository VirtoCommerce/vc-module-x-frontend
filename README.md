# Frontend Experience API

The **X Frontend Module** for Virto Commerce provides a set of optimized backend queries designed specifically to improve **rendering performance** and enhance **front-end user experience** scenarios.  

This module introduces a unified **GraphQL query** that consolidates several commonly requested data types into a single optimized call. This reduces network round-trips and simplifies frontend integration.

---

## Key Features

- **Optimized queries** tailored for frontend rendering needs  
- **Single GraphQL endpoint** providing combined contextual data  
- Fetches aggregated information required to render a page efficiently  
- Extends Virto Commerce GraphQL schema with `pageContext` query  
- Extends Virto Commerce GraphQL schema with `orderStatistics` query: figures over the signed-in user's own orders
- Extends Virto Commerce GraphQL schema with `layout` query and `saveLayout` mutation: the signed-in user's dashboard layouts
- Returns a combined object consisting of:
  - **SlugInfoType**
  - **StoreType**
  - **WhiteLabelingSettingsType**
  - **UserType**

## What the Module Adds

This module introduces the `pageContext` GraphQL query, which returns all necessary page-level contextual information in one call.

### Example Query

```graphql
query GetPageContenxt {
  pageContext (
    domain: "localhost"
    storeId: "Electronics"
    cultureName: "en-US"
    permalink: "/"
    organizationId: "OrganizationId"
    userId: "UserId"
  ) {
    slugInfo {
      entityInfo {
        id
      }
    }
    store {
      storeId
    }
    whiteLabelingSettings {
      logoUrl
    }
    user {
      id  
      userName
    }
  }
}
```

### Response Structure
The `pageContext` field returns an aggregated object designed for fast initial page load.
It contains the following types:

#### SlugInfoType

Provides information about the resolved slug, including the associated entity. Used for determining operation context.

#### StoreType

Contains store-related information such as store ID, settings, and other metadata that may affect rendering.

#### WhiteLabelingSettingsType

Enables dynamic branding in storefronts.

#### UserType

Returns details about the current user

## Order Statistics

The `orderStatistics` query returns figures over the signed-in user's own orders: orders whose customer is the current user, in the organization of the user's token when there is one. Anonymous users and locked or expired accounts are refused. The query has no user, customer or organization argument.

Arguments:

- `storeId`: counts the orders of one store only (all stores when omitted).
- `currencyCode`: currency every amount is converted to. Defaults to the store's default currency, then to the platform's primary currency. An unknown currency code returns an error.
- `cultureName`: culture of the formatted money amounts.

Fields:

- `period(from, to)`: total, count, average, first and last order date of the orders created in the range. Both bounds are inclusive and optional; omit both for all orders.
- `comparison(current, previous)`: the change between two periods, as an amount and in percent. A percent is `null` when the previous value is zero.

Cancelled orders and prototypes (recurring order templates) are not counted. Orders in a currency that is not configured in the platform cannot be converted: they are left out of every figure, and `period` reports them in `excludedCount` and `excludedCurrencies`. `comparison` is computed over the same convertible orders.

```graphql
query {
  orderStatistics(storeId: "B2B-store", currencyCode: "USD", cultureName: "en-US") {
    currencyCode
    yearToDate: period(from: "2026-01-01T00:00:00Z") {
      total { formattedAmount }
      count
      average { formattedAmount }
      excludedCount
      excludedCurrencies
    }
    monthOverMonth: comparison(
      current: { from: "2026-10-01T00:00:00Z" }
      previous: { from: "2026-09-01T00:00:00Z", to: "2026-09-30T23:59:59Z" }
    ) {
      totalChange { formattedAmount }
      totalChangePercent
      countChange
    }
  }
}
```

Each distinct range is aggregated once per request, even when several fields use it. The figures are computed in the database from the Orders module tables, not from the search index.

### Caching

Results are cached per customer for `XFrontend.Statistics.Order.CacheExpirationMinutes` minutes (default 5); `0` turns the cache off. The effective lifetime is the lower of this setting and the platform's `Caching:CacheAbsoluteExpiration`. Saving or deleting one of the customer's orders through the Orders module refreshes that customer's figures on the instance that handled the save, through the platform's per-customer order cache token; other instances of a cluster are refreshed only with the Redis cache backplane. Everything else waits for the expiration, including currency and exchange-rate edits, writes that bypass the Orders module services, and the previous customer of an order moved to another customer.

## Dashboard Layouts

The `layout` query returns the signed-in user's saved layout of a dashboard, or `null` when none was saved. The `saveLayout` mutation replaces it as a whole. Both refuse anonymous users and locked or expired accounts and always work on the current user's own layout.

A layout is stored as a customer preference named `Layout.{scope}`, or `Layout.{scope}.{storeId}` when a store is given, so every user keeps one layout per dashboard and store. The scope is chosen by the storefront, for example `accountDashboard`.

```graphql
mutation {
  saveLayout(command: {
    scope: "accountDashboard"
    storeId: "B2B-store"
    schemaVersion: 1
    regions: [
      { id: "mainLeft", blocks: [
        { id: "recent_orders", type: "recent_orders", hidden: false, settings: [{ key: "maxRows", value: 5 }] }
      ] }
    ]
  }) {
    modifiedDate
  }
}
```

`saveLayout` validates its input:

- `scope`: a letter followed by up to 63 letters or digits. No dots: the preference name joins its parts with dots.
- `storeId`: up to 128 letters, digits, `_` or `-`, naming an existing store.
- Region ids, block ids and block types: 1 to 64 letters, digits, `_` or `-`.
- At most 20 regions, 50 blocks per region and 50 settings per block.
- Setting keys up to 64 characters; each setting value up to 4 KB and the whole layout up to 64 KB as JSON.
- At most 20 layouts per user; saving over an existing layout is always allowed.

## Dependencies

Order statistics read the Orders module tables through its repository, so the module depends on Orders. It also depends on Core (currencies), Customer (layouts are customer preferences), Store (default currency), Profile Experience API and Xapi.

## Usage
This module is intended to be installed in a Virto Commerce backend as part of a frontend integration strategy.
Once enabled, the unified pageContext query becomes available to clients consuming the Virto GraphQL API—typically storefronts, SSR apps, or SPA frameworks.

## License

Copyright (c) Virto Solutions LTD.  All rights reserved.

Licensed under the Virto Commerce Open Software License (the "License"); you
may not use this file except in compliance with the License. You may
obtain a copy of the License at

<https://virtocommerce.com/open-source-license>

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
implied.
