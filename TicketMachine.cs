namespace CTLSAT
{
    class TicketMachine
    {
        private int ticket;

        public TicketMachine()
        {
            ticket = 1;
        }

        public int GetTicket()
        {
            return ticket++;
        }

        public void Reset()
        {
            ticket = 1;
        }
    }

}

