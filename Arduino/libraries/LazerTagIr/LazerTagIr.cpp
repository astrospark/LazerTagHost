#include <LazerTagIr.h>

extern volatile irparams_t irparams;

LazerTagIrReceive::LazerTagIrReceive(int receivePin) : IRrecv(receivePin)
{

}

int LazerTagIrReceive::decode(decode_results *results)
{
	results->rawbuf = irparams.rawbuf;
	results->rawlen = irparams.rawlen;
	if (irparams.rcvstate != STATE_STOP) return ERR;

	if (decodeLazerTag(results)) return DECODED;

	return IRrecv::decode(results);
}

long LazerTagIrReceive::decodeLazerTag(decode_results *results)
{
	if (results->rawlen < 18) return ERR;

	int offset = 1;

	// Presync
	if (!MATCH_MARK(results->rawbuf[offset], LAZERTAG_SIG_PS)) return ERR;
	offset++;

	// Presync Pause
	if (!MATCH_SPACE(results->rawbuf[offset], LAZERTAG_SIG_PSP)) return ERR;
	offset++;

	// Sync
	if (MATCH_MARK(results->rawbuf[offset], LAZERTAG_SIG_TAG_SYNC))
	{
		results->decode_type = TYPE_LAZERTAG_TAG;
	}
	else if (MATCH_MARK(results->rawbuf[offset], LAZERTAG_SIG_BEACON_SYNC))
	{
		results->decode_type = TYPE_LAZERTAG_BEACON;
	}
	else return ERR;
	offset++;

	// Data
	unsigned long data = 0;
	results->bits = 0;
	for (int i = 0; i < (results->rawlen - 4); i++)
	{
		if (i % 2 == 0)
		{
			if (!MATCH_SPACE(results->rawbuf[offset], LAZERTAG_SIG_BIT_PAUSE)) return ERR;
		}
		else
		{
		    if (MATCH_MARK(results->rawbuf[offset], LAZERTAG_SIG_BIT_ZERO))
		    {
			    data <<= 1;
			    data |= 0;
			    results->bits++;
		    }
		    else if (MATCH_MARK(results->rawbuf[offset], LAZERTAG_SIG_BIT_ONE))
		    {
			    data <<= 1;
			    data |= 1;
			    results->bits++;
		    }
		    else return ERR;
		}
		offset++;
	}

	// Success
	results->value = data;
	return DECODED;
}

void LazerTagIrSend::sendLazerTag(unsigned long data, int nbits, bool beacon)
{
	enableIROut(38);

	// Presync
	mark(LAZERTAG_SIG_PS);

	// Presync Pause
	space(LAZERTAG_SIG_PSP);

	// Sync
	if (beacon)
		mark(LAZERTAG_SIG_BEACON_SYNC);
	else
		mark(LAZERTAG_SIG_TAG_SYNC);

	// Pause
	space(LAZERTAG_SIG_BIT_PAUSE);

	// Data
	byte currentBit;
	for (int shift = nbits - 1; shift >= 0; shift--)
	{
		currentBit = (data >> shift) & 0x1;
		if (currentBit)
			mark(LAZERTAG_SIG_BIT_ONE);
		else
			mark(LAZERTAG_SIG_BIT_ZERO);

		space(LAZERTAG_SIG_BIT_PAUSE);
	}

	// Special Format Pause
	space(LAZERTAG_SIG_SFP);
}
