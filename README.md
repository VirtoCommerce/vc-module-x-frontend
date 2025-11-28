# Frontend Experience API

The **X Frontend Module** for Virto Commerce provides a set of optimized backend queries designed specifically to improve **rendering performance** and enhance **front-end user experience** scenarios.  

This module introduces a unified **GraphQL query** that consolidates several commonly requested data types into a single optimized call. This reduces network round-trips and simplifies frontend integration.

---

## Key Features

- **Optimized queries** tailored for frontend rendering needs  
- **Single GraphQL endpoint** providing combined contextual data  
- Fetches aggregated information required to render a page efficiently  
- Extends Virto Commerce GraphQL schema with `pageContext` query  
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
