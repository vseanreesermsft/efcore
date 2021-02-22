// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.TestModels.Northwind;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit;

namespace Microsoft.EntityFrameworkCore.Query
{
    public abstract class NorthwindSelectQueryRelationalTestBase<TFixture> : NorthwindSelectQueryTestBase<TFixture>
        where TFixture : NorthwindQueryFixtureBase<NoopModelCustomizer>, new()
    {
        protected NorthwindSelectQueryRelationalTestBase(TFixture fixture)
            : base(fixture)
        {
        }

        [ConditionalTheory]
        [MemberData(nameof(IsAsyncData))]
        public virtual async Task Correlated_collection_after_groupby_with_complex_projection_not_containing_original_identifier(bool async)
        {
            var filteredOrderIds = new[] { 10248, 10249, 10250 };

            var message = (await Assert.ThrowsAsync<InvalidOperationException>(
              () => AssertQuery(
                async,
                ss => ss.Set<Order>()
                    .GroupBy(o => new { o.CustomerID, Complex = o.OrderDate.Value.Month })
                    .Select(g => new { g.Key, Aggregate = g.Count() })
                    .Select(c => new
                    {
                        c.Key.CustomerID,
                        c.Key.Complex,
                        Subquery = (from x in ss.Set<Order>()
                                    where x.CustomerID == c.Key.CustomerID && filteredOrderIds.Contains(x.OrderID)
                                    select new { Outer = c.Key.CustomerID, Inner = x.OrderID, x.OrderDate }).ToList()
                    }),
                elementSorter: e => (e.CustomerID, e.Complex),
                elementAsserter: (e, a) =>
                {
                    Assert.Equal(e.CustomerID, a.CustomerID);
                    Assert.Equal(e.Complex, a.Complex);
                    AssertCollection(e.Subquery, a.Subquery, elementSorter: ee => ee.Outer);
                }))).Message;

            Assert.Equal(RelationalStrings.InsufficientInformationToIdentifyOuterElementOfCollectionJoin, message);
        }

        public override Task Select_bool_closure_with_order_by_property_with_cast_to_nullable(bool async)
        {
            return AssertTranslationFailed(() => base.Select_bool_closure_with_order_by_property_with_cast_to_nullable(async));
        }

        public override Task Reverse_without_explicit_ordering(bool async)
        {
            return AssertTranslationFailedWithDetails(
                () => base.Reverse_without_explicit_ordering(async), RelationalStrings.MissingOrderingInSelectExpression);
        }

        protected virtual bool CanExecuteQueryString
            => false;

        protected override QueryAsserter CreateQueryAsserter(TFixture fixture)
            => new RelationalQueryAsserter(
                fixture, RewriteExpectedQueryExpression, RewriteServerQueryExpression, canExecuteQueryString: CanExecuteQueryString);
    }
}
