using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using GTA;
using GTA.Native;

namespace iFruitJailbreak
{

    public static class appTextMessage
    {

        public static int Send(string message, int senderID, int recipientID = -1)
        {
            int freeSlot = FindFreeSlot();
            if (freeSlot == -1)
                return -1;

            int targetSlot = GtaMemory.ArrayBase + freeSlot * GtaMemory.SlotSize;
            int phoneOwner = recipientID >= 0
                ? recipientID
                : GlobalVariable.Get(GtaMemory.PhoneOwnerIndex).Read<int>();

            List<string> chunks = SplitMessage(message, GtaMemory.ChunkMaxBytes);
            if (chunks.Count > GtaMemory.MaxChunks)
                chunks = chunks.GetRange(0, GtaMemory.MaxChunks);

            WriteString(targetSlot + 0, "CELL_EMAIL_BCON", 64);

            GlobalVariable.Get(targetSlot + 32).Write<int>(1);

            WriteString(targetSlot + 33, chunks.Count > 0 ? chunks[0] : "", 64);

            int additionalStrings = Math.Max(0, chunks.Count - 1);
            GlobalVariable.Get(targetSlot + 66).Write<int>(additionalStrings);
            WriteString(targetSlot + 67, chunks.Count > 1 ? chunks[1] : "", 64);
            WriteString(targetSlot + 83, chunks.Count > 2 ? chunks[2] : "", 64);

            GlobalVariable.Get(targetSlot + 17).Write<int>(senderID);

            GlobalVariable.Get(GtaMemory.PedHeadshotArray + freeSlot).Write<int>(0);

            DateTime t = World.CurrentDate;
            GlobalVariable.Get(targetSlot + 18).Write<int>(t.Second);
            GlobalVariable.Get(targetSlot + 19).Write<int>(t.Minute);
            GlobalVariable.Get(targetSlot + 20).Write<int>(t.Hour);
            GlobalVariable.Get(targetSlot + 21).Write<int>(t.Day);
            GlobalVariable.Get(targetSlot + 22).Write<int>(t.Month);
            GlobalVariable.Get(targetSlot + 23).Write<int>(t.Year);

            GlobalVariable.Get(targetSlot + 24).Write<int>(1); 
            GlobalVariable.Get(targetSlot + 28).Write<int>(0); 
            GlobalVariable.Get(targetSlot + 99 + 1 + phoneOwner).Write<int>(1);

            GlobalVariable.Get(GtaMemory.TxtMsgFreeIndexGlobal).Write<int>(freeSlot);
            GlobalVariable.Get(GtaMemory.TxtMsgHeadshotIdGlobal).Write<int>(0);
            GlobalVariable.Get(GtaMemory.LastTxtMsgSenderGlobal).Write<int>(senderID);

            int flags = GlobalVariable.Get(GtaMemory.CellphoneFlagsGlobal).Read<int>();
            flags |= (1 << GtaMemory.NewTxtMsgFeedBit) | (1 << GtaMemory.NewTxtMsgDrainBit);
            GlobalVariable.Get(GtaMemory.CellphoneFlagsGlobal).Write<int>(flags);

            Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "Text_Arrive_Tone", "Phone_SoundSet_Default", true);

            return freeSlot;
        }

        public static bool Delete(int slotId)
        {
            if (slotId < 0 || slotId >= GtaMemory.MaxTextMessages)
                return false;

            int targetSlot = GtaMemory.ArrayBase + slotId * GtaMemory.SlotSize;
            if (GlobalVariable.Get(targetSlot + 24).Read<int>() == 0)
                return false;

            GlobalVariable.Get(targetSlot + 24).Write<int>(0); 
            for (int r = 0; r <= 2; r++)
                GlobalVariable.Get(targetSlot + 99 + 1 + r).Write<int>(0);

            return true;
        }

        private static int FindFreeSlot()
        {
            for (int i = 0; i < GtaMemory.MaxTextMessages; i++)
            {
                int slotBase = GtaMemory.ArrayBase + i * GtaMemory.SlotSize;
                if (GlobalVariable.Get(slotBase + 24).Read<int>() == 0)
                    return i;
            }
            return -1;
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

        private static void WriteString(int globalIndex, string text, int maxSize)
        {
            IntPtr addr = GlobalVariable.Get(globalIndex).MemoryAddress;
            byte[] bytes = Encoding.UTF8.GetBytes(text + "\0");
            int length = Math.Min(bytes.Length, maxSize);
            Marshal.Copy(bytes, 0, addr, length);
            if (bytes.Length > maxSize)
                Marshal.WriteByte(addr, maxSize - 1, 0x00);
        }
    }
}
