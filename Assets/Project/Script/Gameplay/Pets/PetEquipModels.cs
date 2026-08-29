using System;
using System.Collections;

namespace PowerMath.Gameplay.Pets
{
    public readonly struct PetEquipCommand
    {
        public PetEquipCommand(string transactionId, string petId, long previewRevision)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
                throw new ArgumentException("Transaction ID is required.", nameof(transactionId));
            if (string.IsNullOrWhiteSpace(petId))
                throw new ArgumentException("Pet ID is required.", nameof(petId));
            if (previewRevision < 0)
                throw new ArgumentOutOfRangeException(nameof(previewRevision));
            TransactionId = transactionId.Trim();
            PetId = petId.Trim();
            PreviewRevision = previewRevision;
        }

        public string TransactionId { get; }
        public string PetId { get; }
        public long PreviewRevision { get; }
    }

    public readonly struct PetEquipReceipt
    {
        public PetEquipReceipt(string transactionId, string petId, long resultingRevision)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
                throw new ArgumentException("Transaction ID is required.", nameof(transactionId));
            if (string.IsNullOrWhiteSpace(petId))
                throw new ArgumentException("Pet ID is required.", nameof(petId));
            if (resultingRevision < 0)
                throw new ArgumentOutOfRangeException(nameof(resultingRevision));
            TransactionId = transactionId.Trim();
            PetId = petId.Trim();
            ResultingRevision = resultingRevision;
        }

        public string TransactionId { get; }
        public string PetId { get; }
        public long ResultingRevision { get; }
    }

    public enum PetEquipFailureCode
    {
        InvalidPet,
        NotOwned,
        StaleState,
        Unavailable,
        RecoverableTransport,
        InvalidSavedState
    }

    public readonly struct PetEquipFailure
    {
        public PetEquipFailure(PetEquipFailureCode code, string message)
        {
            Code = code;
            Message = message ?? string.Empty;
        }

        public PetEquipFailureCode Code { get; }
        public string Message { get; }
    }

    public interface IPetEquipCommandStore
    {
        IEnumerator Equip(
            PetEquipCommand command,
            Action<PetEquipReceipt> completed,
            Action<PetEquipFailure> failed);
    }
}
