namespace AddressRegistry.Projections.Syndication.BuildingUnit
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Be.Vlaanderen.Basisregisters.ProjectionHandling.Syndication;

    public class BuildingUnitAddressMatchProjections : AtomEntryProjectionHandlerModule<BuildingEvent, SyndicationItem<Building>, SyndicationContext>
    {
        public BuildingUnitAddressMatchProjections()
        {
            When(BuildingEvent.BuildingWasRegistered, AddSyndicationItemEntry);
            When(BuildingEvent.BuildingBecameComplete, AddSyndicationItemEntry);
            When(BuildingEvent.BuildingBecameIncomplete, AddSyndicationItemEntry);
            When(BuildingEvent.BuildingUnitAddressWasAttached, AddSyndicationItemEntry);
            When(BuildingEvent.BuildingUnitAddressWasDetached, AddSyndicationItemEntry);
            When(BuildingEvent.BuildingUnitBecameComplete, AddSyndicationItemEntry);
            When(BuildingEvent.BuildingUnitBecameIncomplete, AddSyndicationItemEntry);
            When(BuildingEvent.BuildingUnitOsloIdWasAssigned, AddSyndicationItemEntry);
            When(BuildingEvent.BuildingUnitPositionWasAppointedByAdministrator, AddSyndicationItemEntry);
            When(BuildingEvent.BuildingUnitPositionWasCorrectedToAppointedByAdministrator, AddSyndicationItemEntry);
            When(BuildingEvent.BuildingUnitPositionWasCorrectedToDerivedFromObject, AddSyndicationItemEntry);
            When(BuildingEvent.BuildingUnitPositionWasDerivedFromObject, AddSyndicationItemEntry);
            When(BuildingEvent.BuildingUnitStatusWasRemoved, AddSyndicationItemEntry);
            When(BuildingEvent.BuildingUnitWasAdded, AddSyndicationItemEntry);
            When(BuildingEvent.BuildingUnitWasAddedToRetiredBuilding, AddSyndicationItemEntry);
            When(BuildingEvent.BuildingUnitWasCorrectedToNotRealized, AddSyndicationItemEntry);
            When(BuildingEvent.BuildingUnitWasCorrectedToPlanned, AddSyndicationItemEntry);
            When(BuildingEvent.BuildingUnitWasCorrectedToRealized, AddSyndicationItemEntry);
            When(BuildingEvent.BuildingUnitWasCorrectedToRetired, AddSyndicationItemEntry);
            When(BuildingEvent.BuildingUnitWasNotRealized, AddSyndicationItemEntry);
            When(BuildingEvent.BuildingUnitWasNotRealizedByBuilding, AddSyndicationItemEntry);
            When(BuildingEvent.BuildingUnitWasNotRealizedByParent, AddSyndicationItemEntry);
            When(BuildingEvent.BuildingUnitWasPlanned, AddSyndicationItemEntry);
            When(BuildingEvent.BuildingUnitWasReaddedByOtherUnitRemoval, AddSyndicationItemEntry);
            When(BuildingEvent.BuildingUnitWasReaddressed, AddSyndicationItemEntry);
            When(BuildingEvent.BuildingUnitWasRealized, AddSyndicationItemEntry);
            When(BuildingEvent.BuildingUnitWasRemoved, AddSyndicationItemEntry);
            When(BuildingEvent.BuildingUnitWasRetired, AddSyndicationItemEntry);
            When(BuildingEvent.BuildingUnitWasRetiredByParent, AddSyndicationItemEntry);
            When(BuildingEvent.CommonBuildingUnitWasAdded, AddSyndicationItemEntry);
            When(BuildingEvent.BuildingWasRemoved, RemoveBuilding);
        }

        private static async Task RemoveBuilding(AtomEntry<SyndicationItem<Building>> entry, SyndicationContext context, CancellationToken ct)
        {
            var buildingUnitAddressMatchLatestItems =
                context
                    .BuildingUnitAddressMatchLatestItems
                    .Where(x => x.BuildingId == entry.Content.Object.Id)
                    .Concat(context.BuildingUnitAddressMatchLatestItems.Local.Where(x => x.BuildingId == entry.Content.Object.Id));

            context.BuildingUnitAddressMatchLatestItems.RemoveRange(buildingUnitAddressMatchLatestItems);
        }

        private static async Task AddSyndicationItemEntry(AtomEntry<SyndicationItem<Building>> entry, SyndicationContext context, CancellationToken ct)
        {
            var buildingUnitAddressMatchLatestItems =
                context
                    .BuildingUnitAddressMatchLatestItems
                    .Where(x => x.BuildingId == entry.Content.Object.Id)
                    .Concat(context.BuildingUnitAddressMatchLatestItems.Local.Where(x => x.BuildingId == entry.Content.Object.Id))
                    .Distinct()
                    .ToList();

            var itemsToRemove = new List<BuildingUnitAddressMatchLatestItem>();
            foreach (var buildingUnitAddressMatchLatestItem in buildingUnitAddressMatchLatestItems)
            {
                if (!entry.Content.Object.BuildingUnits.Select(x => x.BuildingUnitId).Contains(buildingUnitAddressMatchLatestItem.BuildingUnitId))
                    itemsToRemove.Add(buildingUnitAddressMatchLatestItem);
            }

            context.BuildingUnitAddressMatchLatestItems.RemoveRange(itemsToRemove);

            foreach (var buildingUnit in entry.Content.Object.BuildingUnits)
            {
                var unitItems = buildingUnitAddressMatchLatestItems.Where(x => x.BuildingUnitId == buildingUnit.BuildingUnitId).ToList();
                var addressItemsToRemove = unitItems.Where(x => !buildingUnit.Addresses.Contains(x.AddressId));
                foreach (var addressId in buildingUnit.Addresses)
                {
                    var addressItem = unitItems.FirstOrDefault(x => x.AddressId == addressId);
                    if (addressItem == null)
                    {
                        await context.BuildingUnitAddressMatchLatestItems.AddAsync(
                            new BuildingUnitAddressMatchLatestItem
                            {
                                AddressId = addressId,
                                BuildingId = entry.Content.Object.Id,
                                BuildingUnitOsloId = buildingUnit.Identificator.ObjectId,
                                BuildingUnitId = buildingUnit.BuildingUnitId,
                                IsComplete = buildingUnit.IsComplete,
                            }, ct);
                    }
                    else
                    {
                        addressItem.BuildingUnitOsloId = buildingUnit.Identificator.ObjectId;
                        addressItem.IsComplete = buildingUnit.IsComplete;
                    }
                }

                context.BuildingUnitAddressMatchLatestItems.RemoveRange(addressItemsToRemove);
            }
        }
    }
}
