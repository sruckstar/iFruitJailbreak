using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using GTA;
using GTA.Native;

namespace iFruitJailbreak
{
    public static class appMail
    {
        public static void Send(string header, string body, string senderName, int recipientID = -1,
            string senderIcon = "CHAR_DEFAULT", int headerLabelId = -1, int bodyLabelId = -1)
        {
            WriteString(
                GtaMemory.CharSheetBase + 1 + GtaMemory.NoCharacterSenderId * GtaMemory.CharSheetStride + 3,
                senderName, 64);

            WriteString(
                GtaMemory.CharSheet145IconGlobal,
                string.IsNullOrEmpty(senderIcon) ? "CHAR_DEFAULT" : senderIcon, 64);

            InjectEntry(
                headerLabelId >= 0 ? headerLabelId : GtaMemory.EmailCustomHeaderStringSlot,
                bodyLabelId   >= 0 ? bodyLabelId   : GtaMemory.EmailCustomBodyStringSlot,
                GtaMemory.NoCharacterSenderId,
                recipientID);

            RaiseEmailFeed(GtaMemory.NoCharacterSenderId, senderName, senderIcon, header, recipientID);
        }

        public static void SendStrID(int headerID, int bodyID, int senderID, int recipientID = -1, string feedSubject = null)
        {
            InjectEntry(headerID, bodyID, senderID, recipientID);

            
            
            string iconTxd  = EmailSenderMap.IconTxd(senderID);   
            string nameText = EmailSenderMap.NameText(senderID); 

            string subject = feedSubject ?? EmailStr.Text(headerID);

            RaiseEmailFeed(GtaMemory.NoCharacterSenderId, nameText, iconTxd, subject, recipientID);
        }

        private static void InjectEntry(int headerID, int bodyID, int senderID, int recipientID)
        {
            int playerIdx = recipientID >= 0
                ? recipientID
                : GlobalVariable.Get(GtaMemory.PhoneOwnerIndex).Read<int>();

            int defBase = GtaMemory.EmailData + 1 + GtaMemory.EmailCustomDefSlot * GtaMemory.EmailDefStride;
            GlobalVariable.Get(defBase + 0).Write<int>(bodyID);  
            GlobalVariable.Get(defBase + 1).Write<int>(headerID); 
            GlobalVariable.Get(defBase + 2).Write<int>(senderID);
            GlobalVariable.Get(defBase + 3).Write<int>(0);
            GlobalVariable.Get(defBase + 4).Write<int>(1); 

            int campBase = GtaMemory.EmailCampaigns + 1 + GtaMemory.EmailCustomCampaignSlot * GtaMemory.EmailCampaignStride;
            GlobalVariable.Get(campBase + 0).Write<int>(1); 
            GlobalVariable.Get(campBase + 1).Write<int>(0);
            GlobalVariable.Get(campBase + 33).Write<int>(GtaMemory.EmailCustomDefSlot);
            GlobalVariable.Get(campBase + 42).Write<int>(1);

            int inboxBase = GtaMemory.EmailInbox + 1 + playerIdx * GtaMemory.EmailInboxStride;
            int emailCount = GlobalVariable.Get(inboxBase + 0).Read<int>();
            int slot = emailCount % 16;

            GlobalVariable.Get(inboxBase + 2  + slot).Write<int>(0);
            GlobalVariable.Get(inboxBase + 19 + slot).Write<int>(GtaMemory.EmailCustomCampaignSlot);
            GlobalVariable.Get(inboxBase + 36 + slot).Write<int>(0);
            GlobalVariable.Get(inboxBase + 70 + slot).Write<int>(0); 
            GlobalVariable.Get(inboxBase + 87 + slot).Write<int>(0); 
            GlobalVariable.Get(inboxBase + 104 + slot).Write<int>(0); 
            GlobalVariable.Get(inboxBase + 0).Write<int>(emailCount + 1);
        }

        private static void RaiseEmailFeed(int senderId, string senderName, string senderIcon, string subject, int recipientID)
        {
            int owner = recipientID >= 0
                ? recipientID
                : GlobalVariable.Get(GtaMemory.PhoneOwnerIndex).Read<int>();

            int freeSlot = FindFreeEmailFeedSlot();
            int slotBase = GtaMemory.EmailFeedArrayBase + freeSlot * GtaMemory.EmailFeedSlotSize;

            List<string> chunks = SplitMessage(subject ?? "", GtaMemory.ChunkMaxBytes);
            if (chunks.Count > GtaMemory.MaxChunks)
                chunks = chunks.GetRange(0, GtaMemory.MaxChunks);

            WriteString(slotBase + 0, "CELL_EMAIL_BCON", 64);

            GlobalVariable.Get(slotBase + 32).Write<int>(1);

            WriteString(slotBase + 33, chunks.Count > 0 ? chunks[0] : " ", 64);
            WriteString(slotBase + 67, chunks.Count > 1 ? chunks[1] : " ", 64);
            WriteString(slotBase + 83, chunks.Count > 2 ? chunks[2] : " ", 64);
            GlobalVariable.Get(slotBase + 66).Write<int>(2);

            GlobalVariable.Get(slotBase + 17).Write<int>(senderId);

            if (senderId == GtaMemory.NoCharacterSenderId)
            {
                WriteString(GtaMemory.CustomSenderNameGlobal,
                    string.IsNullOrEmpty(senderName) ? " " : senderName, 64);

                WriteString(GtaMemory.CharSheet145IconGlobal,
                    string.IsNullOrEmpty(senderIcon) ? "CHAR_DEFAULT" : senderIcon, 64);
            }

            GlobalVariable.Get(slotBase + 24).Write<int>(1); 
            GlobalVariable.Get(slotBase + 28).Write<int>(0); 
            GlobalVariable.Get(slotBase + 291 + 1 + owner).Write<int>(1); 

            GlobalVariable.Get(GtaMemory.EmailFeedFreeIndexGlobal).Write<int>(freeSlot); 
            GlobalVariable.Get(GtaMemory.TxtMsgHeadshotIdGlobal).Write<int>(0);          

            int tu = GlobalVariable.Get(GtaMemory.CellphoneTuBitsetGlobal).Read<int>();
            tu |= (1 << GtaMemory.DisplayEmailSignifierBit);
            GlobalVariable.Get(GtaMemory.CellphoneTuBitsetGlobal).Write<int>(tu);

            Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "Text_Arrive_Tone", "Phone_SoundSet_Default", true);
        }

        private static int FindFreeEmailFeedSlot()
        {
            for (int i = 0; i < GtaMemory.EmailFeedMaxSlots; i++)
            {
                int slotBase = GtaMemory.EmailFeedArrayBase + i * GtaMemory.EmailFeedSlotSize;
                if (GlobalVariable.Get(slotBase + 24).Read<int>() == 0)
                    return i;
            }
            return 0;
        }

        private static List<string> SplitMessage(string text, int maxBytes)
        {
            var chunks = new List<string>();
            string current = "";

            foreach (char c in text)
            {
                if (Encoding.UTF8.GetByteCount(current + c) <= maxBytes)
                    current += c;
                else
                {
                    chunks.Add(current);
                    current = c.ToString();
                }
            }

            if (!string.IsNullOrEmpty(current))
                chunks.Add(current);

            return chunks;
        }

        private static void WriteString(int globalIndex, string text, int maxBytes)
        {
            IntPtr addr = GlobalVariable.Get(globalIndex).MemoryAddress;
            byte[] bytes = Encoding.UTF8.GetBytes(text + "\0");
            int length = Math.Min(bytes.Length, maxBytes);
            Marshal.Copy(bytes, 0, addr, length);
            if (bytes.Length > maxBytes)
                Marshal.WriteByte(addr, maxBytes - 1, 0x00);
        }
    }

    internal static class EmailSenderMap
    {
        private static string CharIcon(int charIdx)
        {
            int g = GtaMemory.CharSheetBase + 1 + charIdx * GtaMemory.CharSheetStride + 7;
            IntPtr addr = GlobalVariable.Get(g).MemoryAddress;
            return Marshal.PtrToStringAnsi(addr) ?? "";
        }

        internal static string IconTxd(int senderID)
        {
            switch (senderID)
            {
                case 0:  return CharIcon(0);
                case 1:  return CharIcon(1);
                case 2:  return CharIcon(2);
                case 7:  return CharIcon(12);
                case 4:  return CharIcon(60);
                case 6:  return CharIcon(62);
                case 3:  return CharIcon(14);
                case 16: return CharIcon(97);
                case 19: return CharIcon(99);
                case 15: return CharIcon(96);
                case 63: return "CHAR_CARSITE2";
                case 64: return "CHAR_BOATSITE";
                case 8:  return "CHAR_BANK_MAZE";
                case 9:  return "CHAR_BANK_FLEECA";
                case 10: return "CHAR_BANK_BOL";
                case 21: return "CHAR_MINOTAUR";
                case 25: return CharIcon(15);
                case 26: return CharIcon(30);
                case 27: return CharIcon(17);
                case 29: return CharIcon(20);
                case 30: return CharIcon(43);
                case 31: return CharIcon(44);
                case 32: return CharIcon(19);
                case 34: return CharIcon(40);
                case 36: return "CELL_E_381";
                case 38: return CharIcon(64);
                case 5:  return "CHAR_EPSILON";
                case 13: return "CHAR_MILSITE";
                case 11: return "CHAR_CARSITE";
                case 14: return "CHAR_BOATSITE";
                case 12: return "CHAR_PLANESITE";
                case 24: return "CHAR_DR_FRIEDLANDER";
                case 55: return "CHAR_CARSITE2";
                case 54: return "CHAR_BIKESITE";
                case 39: return CharIcon(122);
                case 40: return CharIcon(125);
                case 41: return CharIcon(113);
                case 42: return CharIcon(126);
                case 43: return CharIcon(127);
                case 44: return CharIcon(124);
                case 45: return CharIcon(114);
                case 46: return CharIcon(115);
                case 47: return CharIcon(116);
                case 48: return CharIcon(123);
                case 49: return CharIcon(117);
                case 50: return CharIcon(118);
                case 51: return CharIcon(119);
                case 52: return CharIcon(120);
                case 53: return CharIcon(121);
                default: return "CHAR_DEFAULT"; 
            }
        }

        internal static string NameLabel(int senderID)
        {
            switch (senderID)
            {
                case 0:  return "EMSTR_1";
                case 3:  return "EMSTR_4";
                case 1:  return "EMSTR_7";
                case 2:  return "EMSTR_10";
                case 4:  return "EMSTR_13";
                case 5:  return "EMSTR_30";
                case 6:  return "EMSTR_37";
                case 7:  return "EMSTR_40";
                case 8:  return "EMSTR_53";
                case 9:  return "EMSTR_56";
                case 10: return "EMSTR_59";
                case 11: return "EMSTR_79";
                case 12: return "EMSTR_82";
                case 13: return "EMSTR_85";
                case 14: return "EMSTR_88";
                case 15: return "EMSTR_107";
                case 16: return "EMSTR_115";
                case 17: return "EMSTR_143";
                case 18: return "EMSTR_146";
                case 19: return "EMSTR_153";
                case 20: return "EMSTR_158";
                case 21: return "EMSTR_164";
                case 22: return "EMSTR_183";
                case 23: return "EMSTR_188";
                case 24: return "EMSTR_191";
                case 25: return "EMSTR_207";
                case 26: return "EMSTR_220";
                case 27: return "EMSTR_227";
                case 28: return "EMSTR_234";
                case 29: return "EMSTR_243";
                case 30: return "EMSTR_250";
                case 31: return "EMSTR_263";
                case 32: return "EMSTR_270";
                case 33: return "EMSTR_320";
                case 34: return "EMSTR_341";
                case 35: return "EMSTR_349";
                case 36: return "EMSTR_353";
                case 37: return "EMSTR_358";
                case 38: return "EMSTR_361";
                case 39: return "EMSTR_370";
                case 40: return "EMSTR_377";
                case 41: return "EMSTR_380";
                case 42: return "EMSTR_383";
                case 43: return "EMSTR_385";
                case 44: return "EMSTR_388";
                case 45: return "EMSTR_391";
                case 46: return "EMSTR_394";
                case 47: return "EMSTR_397";
                case 48: return "EMSTR_400";
                case 49: return "EMSTR_403";
                case 50: return "EMSTR_406";
                case 51: return "EMSTR_409";
                case 52: return "EMSTR_412";
                case 53: return "EMSTR_415";
                case 54: return "EMSTR_466";
                case 55: return "EMSTR_469";
                case 56: return "EMSTR_490";
                case 57: return "EMSTR_493";
                case 58: return "EMSTR_496";
                case 59: return "EMSTR_499";
                case 60: return "EMSTR_502";
                case 61: return "EMSTR_505";
                case 62: return "EMSTR_508";
                case 63: return "EMSTR_641";
                case 64: return "EMSTR_644";
                case 65: return "EMSTR_653";
                default: return "NULL";
            }
        }

        internal static string NameText(int senderID)
        {
            string label = NameLabel(senderID);
            if (string.IsNullOrEmpty(label) || label == "NULL")
                return " ";
            string text = Game.GetLocalizedString(label);
            return string.IsNullOrEmpty(text) ? " " : text;
        }
    }

    internal static class EmailStr
    {
        internal static string Label(int id)
        {
            return id > -1 ? "EMSTR_" + id : "FAIL";
        }

        internal static string Text(int id)
        {
            if (id < 0) return "";
            string text = Game.GetLocalizedString(Label(id));
            return text ?? "";
        }
    }
}
