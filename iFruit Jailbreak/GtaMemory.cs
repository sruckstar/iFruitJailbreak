namespace iFruitJailbreak
{
    internal static class GtaMemory
    {
        internal const int SavedGlobalsBase = 114904;
        internal const int TextMessageArrayOffset = 14148;
        internal const int ArrayBase = SavedGlobalsBase + TextMessageArrayOffset + 1;

        internal const int PhoneOwnerIndex = 21610;
        internal const int CharSheetBase = 2339;
        internal const int CharSheetStride = 29;
        internal const int PedHeadshotArray = 24053;

        internal const int MaxTextMessages = 165;
        internal const int SlotSize = 104;
        internal const int ChunkMaxBytes = 63;
        internal const int MaxChunks = 3;

        internal const int NoCharacterSenderId = 145;

        internal const int CharSheet145IconGlobal = CharSheetBase + 1 + 145 * CharSheetStride + 7; // 6552

        internal const int TxtMsgFreeIndexGlobal = 24051;

        internal const int TxtMsgHeadshotIdGlobal = 24044;

        internal const int LastTxtMsgSenderGlobal = 10167;

        internal const int CellphoneFlagsGlobal = 9463;
        internal const int NewTxtMsgFeedBit = 1;
        internal const int NewTxtMsgDrainBit = 7;

        internal const int CustomSenderNameGlobal = 10168;

        internal const int EmailFeedArrayBase = 4521275 + 1; 
        internal const int EmailFeedSlotSize = 296;
        internal const int EmailFeedMaxSlots = 12;

        internal const int EmailFeedFreeIndexGlobal = 4524828;

        internal const int CellphoneTuBitsetGlobal = 4524844;
        internal const int DisplayEmailSignifierBit = 0;

        internal const int EmailUnreadCountBase = 46105;
        internal const int EmailData = 46113;     
        internal const int EmailCampaigns = 49438; 
        internal const int EmailInbox = 55051;  

        internal const int EmailDefStride = 12;
        internal const int EmailCampaignStride = 46;
        internal const int EmailInboxStride = 120;

        internal const int EmailCustomDefSlot = 180;
        internal const int EmailCustomCampaignSlot = 110;

        internal const int EmailCustomHeaderStringSlot = 900;
        internal const int EmailCustomBodyStringSlot = 901;
    }
}
