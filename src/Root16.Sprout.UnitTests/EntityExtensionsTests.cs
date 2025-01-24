using System.ComponentModel;
using Microsoft.Xrm.Sdk;
using Root16.Sprout.DataSources.Dataverse;

namespace Root16.Sprout.UnitTests
{
    public class EntityExtensionsTests
    {
        #region EntityReferenceCollection Tests
        [Fact]
        public void CloneWithModifiedAttributes_ShouldDetectDifferentNumberOfGroups()
        {
            var originalCollection = new EntityReferenceCollection
            {
                new EntityReference("contact", Guid.NewGuid())
            };

            var updateCollection = new EntityReferenceCollection
            {
                new EntityReference("contact", Guid.NewGuid()),
                new EntityReference("account", Guid.NewGuid())
            };

            var original = new Entity("account", Guid.NewGuid())
            {
                Attributes = new Microsoft.Xrm.Sdk.AttributeCollection
                {
                    { "relatedEntities", originalCollection }
                }
            };

            var updates = new Entity("account", original.Id)
            {
                Attributes = new Microsoft.Xrm.Sdk.AttributeCollection
                {
                    { "relatedEntities", updateCollection }
                }
            };

            var delta = updates.CloneWithModifiedAttributes(original);

            Assert.Single(delta.Attributes);
            Assert.Equal(updateCollection, delta["relatedEntities"]);
        }

        [Fact]
        public void CloneWithModifiedAttributes_ShouldDetectDifferentGroupTypes()
        {
            var originalCollection = new EntityReferenceCollection
            {
                new EntityReference("contact", Guid.NewGuid())
            };

            var updateCollection = new EntityReferenceCollection
            {
                new EntityReference("account", Guid.NewGuid())
            };

            var original = new Entity("account", Guid.NewGuid())
            {
                Attributes = new Microsoft.Xrm.Sdk.AttributeCollection
                {
                    { "relatedEntities", originalCollection }
                }
            };

            var updates = new Entity("account", original.Id)
            {
                Attributes = new Microsoft.Xrm.Sdk.AttributeCollection
                {
                    { "relatedEntities", updateCollection }
                }
            };

            var delta = updates.CloneWithModifiedAttributes(original);

            Assert.Single(delta.Attributes);
            Assert.Equal(updateCollection, delta["relatedEntities"]);
        }

        [Fact]
        public void CloneWithModifiedAttributes_ShouldDetectDifferentNumberOfRecordsInGroups()
        {
            var originalCollection = new EntityReferenceCollection
            {
                new EntityReference("contact", Guid.NewGuid())
            };

            var updateCollection = new EntityReferenceCollection
            {
                new EntityReference("contact", Guid.NewGuid()),
                new EntityReference("contact", Guid.NewGuid())
            };

            var original = new Entity("account", Guid.NewGuid())
            {
                Attributes = new Microsoft.Xrm.Sdk.AttributeCollection
                {
                    { "relatedEntities", originalCollection }
                }
            };

            var updates = new Entity("account", original.Id)
            {
                Attributes = new Microsoft.Xrm.Sdk.AttributeCollection
                {
                    { "relatedEntities", updateCollection }
                }
            };

            var delta = updates.CloneWithModifiedAttributes(original);

            Assert.Single(delta.Attributes);
            Assert.Equal(updateCollection, delta["relatedEntities"]);
        }

        [Fact]
        public void CloneWithModifiedAttributes_ShouldDetectDifferentRecordsInGroups()
        {
            var originalCollection = new EntityReferenceCollection
            {
                new EntityReference("contact", Guid.NewGuid()),
                new EntityReference("contact", Guid.NewGuid())
            };

            var updateCollection = new EntityReferenceCollection
            {
                new EntityReference("contact", originalCollection[0].Id),
                new EntityReference("contact", Guid.NewGuid()) // Different ID
            };

            var original = new Entity("account", Guid.NewGuid())
            {
                Attributes = new Microsoft.Xrm.Sdk.AttributeCollection
                {
                    { "relatedEntities", originalCollection }
                }
            };

            var updates = new Entity("account", original.Id)
            {
                Attributes = new Microsoft.Xrm.Sdk.AttributeCollection
                {
                    { "relatedEntities", updateCollection }
                }
            };

            var delta = updates.CloneWithModifiedAttributes(original);

            Assert.Single(delta.Attributes);
            Assert.Equal(updateCollection, delta["relatedEntities"]);
        }

        [Fact]
        public void CloneWithModifiedAttributes_ShouldReturnEmptyDelta_WhenRecordsAreSame()
        {
            var originalCollection = new EntityReferenceCollection
            {
                new EntityReference("contact", Guid.NewGuid()),
                new EntityReference("contact", Guid.NewGuid())
            };

            var updateCollection = new EntityReferenceCollection
            {
                new EntityReference("contact", originalCollection[0].Id),
                new EntityReference("contact", originalCollection[1].Id) // Same IDs
            };

            var original = new Entity("account", Guid.NewGuid())
            {
                Attributes = new Microsoft.Xrm.Sdk.AttributeCollection
                {
                    { "relatedEntities", originalCollection }
                }
            };

            var updates = new Entity("account", original.Id)
            {
                Attributes = new Microsoft.Xrm.Sdk.AttributeCollection
                {
                    { "relatedEntities", updateCollection }
                }
            };

            var delta = updates.CloneWithModifiedAttributes(original);

            Assert.Empty(delta.Attributes);
        }
        #endregion

        #region EntityCollection Tests
        [Fact]
        public void CloneWithModifiedAttributes_EntityCollection_SamePartyIds()
        {
            var original = new Entity("test_entity", Guid.NewGuid())
            {
                Attributes = new Microsoft.Xrm.Sdk.AttributeCollection
                {
                    { "partylist", new EntityCollection(new List<Entity>
                        {
                            new Entity("party") { ["partyid"] = new EntityReference("party", Guid.NewGuid()) },
                            new Entity("party") { ["partyid"] = new EntityReference("party", Guid.NewGuid()) }
                        })
                    }
                }
            };

            var updates = new Entity("test_entity", original.Id)
            {
                Attributes = new Microsoft.Xrm.Sdk.AttributeCollection
                {
                    { "partylist", new EntityCollection(new List<Entity>
                        {
                            new Entity("party") { ["partyid"] = original.GetAttributeValue<EntityCollection>("partylist").Entities[0].GetAttributeValue<EntityReference>("partyid") },
                            new Entity("party") { ["partyid"] = original.GetAttributeValue<EntityCollection>("partylist").Entities[1].GetAttributeValue<EntityReference>("partyid") }
                        })
                    }
                }
            };

            var delta = updates.CloneWithModifiedAttributes(original);

            Assert.Empty(delta.Attributes);
        }

        [Fact]
        public void CloneWithModifiedAttributes_EntityCollection_DifferentPartyIds()
        {
            var original = new Entity("test_entity", Guid.NewGuid())
            {
                Attributes = new Microsoft.Xrm.Sdk.AttributeCollection
                {
                    { "partylist", new EntityCollection(new List<Entity>
                        {
                            new Entity("party") { ["partyid"] = new EntityReference("party", Guid.NewGuid()) },
                            new Entity("party") { ["partyid"] = new EntityReference("party", Guid.NewGuid()) }
                        })
                    }
                }
            };

            var updates = new Entity("test_entity", original.Id)
            {
                Attributes = new Microsoft.Xrm.Sdk.AttributeCollection
                {
                    { "partylist", new EntityCollection(new List<Entity>
                        {
                            new Entity("party") { ["partyid"] = new EntityReference("party", Guid.NewGuid()) },
                            new Entity("party") { ["partyid"] = new EntityReference("party", Guid.NewGuid()) }
                        })
                    }
                }
            };

            var delta = updates.CloneWithModifiedAttributes(original);

            Assert.Single(delta.Attributes);
            Assert.True(delta.Attributes.ContainsKey("partylist"));
        }

        [Fact]
        public void CloneWithModifiedAttributes_EntityCollection_EmptyOriginal()
        {
            var original = new Entity("test_entity", Guid.NewGuid())
            {
                Attributes = new Microsoft.Xrm.Sdk.AttributeCollection
                {
                    { "partylist", new EntityCollection(new List<Entity>()) }
                }
            };

            var updates = new Entity("test_entity", original.Id)
            {
                Attributes = new Microsoft.Xrm.Sdk.AttributeCollection
                {
                    { "partylist", new EntityCollection(new List<Entity>
                        {
                            new Entity("party") { ["partyid"] = new EntityReference("party", Guid.NewGuid()) }
                        })
                    }
                }
            };

            var delta = updates.CloneWithModifiedAttributes(original);

            Assert.Single(delta.Attributes);
            Assert.True(delta.Attributes.ContainsKey("partylist"));
        }

        [Fact]
        public void CloneWithModifiedAttributes_EntityCollection_EmptyUpdates()
        {
            var original = new Entity("test_entity", Guid.NewGuid())
            {
                Attributes = new Microsoft.Xrm.Sdk.AttributeCollection
                {
                    { "partylist", new EntityCollection(new List<Entity>
                        {
                            new Entity("party") { ["partyid"] = new EntityReference("party", Guid.NewGuid()) }
                        })
                    }
                }
            };

            var updates = new Entity("test_entity", original.Id)
            {
                Attributes = new Microsoft.Xrm.Sdk.AttributeCollection
                {
                    { "partylist", new EntityCollection(new List<Entity>()) }
                }
            };

            var delta = updates.CloneWithModifiedAttributes(original);

            Assert.Single(delta.Attributes);
            Assert.True(delta.Attributes.ContainsKey("partylist"));
        }
        #endregion


        #region DateTime Tests
        [Fact]
        public void CloneWithModifiedAttributes_ShouldDetectDifferentDateTimeValues()
        {
            var original = new Entity("account", Guid.NewGuid())
            {
                Attributes = new Microsoft.Xrm.Sdk.AttributeCollection
                {
                    { "modifiedon", new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc) }
                }
            };

            var updates = new Entity("account", original.Id)
            {
                Attributes = new Microsoft.Xrm.Sdk.AttributeCollection
                {
                    { "modifiedon", new DateTime(2023, 1, 2, 12, 0, 0, DateTimeKind.Utc) }
                }
            };

            var delta = updates.CloneWithModifiedAttributes(original);

            Assert.Single(delta.Attributes);
            Assert.Equal(new DateTime(2023, 1, 2, 12, 0, 0, DateTimeKind.Utc), delta["modifiedon"]);
        }

        [Fact]
        public void CloneWithModifiedAttributes_ShouldReturnEmptyDelta_WhenDateTimeValuesAreSame()
        {
            var original = new Entity("account", Guid.NewGuid())
            {
                Attributes = new Microsoft.Xrm.Sdk.AttributeCollection
                {
                    { "modifiedon", new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc) }
                }
            };

            var updates = new Entity("account", original.Id)
            {
                Attributes = new Microsoft.Xrm.Sdk.AttributeCollection
                {
                    { "modifiedon", new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc) }
                }
            };

            var delta = updates.CloneWithModifiedAttributes(original);

            Assert.Empty(delta.Attributes);
        }

        [Fact]
        public void CloneWithModifiedAttributes_ShouldDetectDifferentDateTimeValuesWithDifferentTimeZones()
        {
            var originalDateTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var localTimeZone = TimeZoneInfo.Local;
            var updateDateTime = TimeZoneInfo.ConvertTimeToUtc(new DateTime(2023, 1, 1, 6, 0, 0, DateTimeKind.Local), localTimeZone);

            var original = new Entity("account", Guid.NewGuid())
            {
                Attributes = new Microsoft.Xrm.Sdk.AttributeCollection
                {
                    { "modifiedon", originalDateTime }
                }
            };

            var updates = new Entity("account", original.Id)
            {
                Attributes = new Microsoft.Xrm.Sdk.AttributeCollection
                {
                    { "modifiedon", updateDateTime }
                }
            };

            var delta = updates.CloneWithModifiedAttributes(original);

            Assert.Empty(delta.Attributes);
        }

        [Fact]
        public void CloneWithModifiedAttributes_ShouldDetectNullOriginalDateTime()
        {
            var original = new Entity("account", Guid.NewGuid())
            {
                Attributes = new Microsoft.Xrm.Sdk.AttributeCollection
                {
                    { "modifiedon", null }
                }
            };

            var updates = new Entity("account", original.Id)
            {
                Attributes = new Microsoft.Xrm.Sdk.AttributeCollection
                {
                    { "modifiedon", new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc) }
                }
            };

            var delta = updates.CloneWithModifiedAttributes(original);

            Assert.Single(delta.Attributes);
            Assert.Equal(new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc), delta["modifiedon"]);
        }

        [Fact]
        public void CloneWithModifiedAttributes_ShouldDetectNullUpdateDateTime()
        {
            var original = new Entity("account", Guid.NewGuid())
            {
                Attributes = new Microsoft.Xrm.Sdk.AttributeCollection
                {
                    { "modifiedon", new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc) }
                }
            };

            var updates = new Entity("account", original.Id)
            {
                Attributes = new Microsoft.Xrm.Sdk.AttributeCollection
                {
                    { "modifiedon", null }
                }
            };

            var delta = updates.CloneWithModifiedAttributes(original);

            Assert.Single(delta.Attributes);
            Assert.Null(delta["modifiedon"]);
        }
        #endregion
    }
}