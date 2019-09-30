# Mocks vs Decomposition

Mock-heavy unit tests can be fragile and might have a large and confusing setup. A proper decomposition to (as close as possible) pure functions tends to give much better results long-term. Or does it?

## Requirements
Given a list of products, determine which exceed the maximum allowed weight and return the excess.

## Business rules
* `Special` products are exempt from the weight calculation.
* Maximum weight depends on product type and category, and is provided by the external API.
* There is a tolerance of 5kg (e.g. excess smaller than that is not counted).

## Implementation considerations
* Load the external API as little as possible.
* Provide results as fast as possible.