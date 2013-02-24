/* 
 * (C) 2013 Scott Willeke (scott@willeke.com).
 *
 * This is free software; you can redistribute it and/or modify it under
 * the terms of the GNU Lesser General Public License (LGPL) version 2.1
 *
 * This file is for a .NET-friendly wrapper for the CAB decompression code.
 * Unfortunately it isn't possible/feasible to return a struct with methods on it from C (as is done on the mscab_decompressor and other structs).
 * So the code in here works around it.
 * 
 * 
 * 
 * My goals with lessmsi is to be able to decompress a cab file-by-file. So the only functions I really need are as follows:
 * mscab_decompressor mspack_create_cab_decompressor(*blah)	- can be called directly from .NET
 * void mspack_create_cab_decompressor(mscab_decompressor self)	- can be called directly from .NET
 * 
 * mscabd_cabinet* 	mscab_decompressor_open			(self, const char* filename)									- Opens a cabinet file and reads its contents. 
 * void 			mscab_decompressor_close		(struct mscab_decompressor *self, struct mscabd_cabinet *cab)	- 
 * int*				mscab_decompressor_extract		(struct mscab_decompressor *self, struct mscabd_file *file, const char *filename) - Extracts a file from a cabinet or cabinet set. 
 * int* 			mscab_decompressor_last_error	(struct mscab_decompressor *self)								- Returns the error code set by the most recently called method. 
 * 
 */
 
#include <stdio.h>
#include <mspack.h>
#include <cab.h>
#include <stdarg.h>


void trace(struct mscab_decompressor *self, const char *format, ...) {
	struct mscab_decompressor_p* selfp;
	struct mspack_system *sys;
	va_list ap;
	char buffer[255*2];
	
	selfp = (struct mscab_decompressor_p*) self;
	sys = selfp->system;	
	va_start(ap, format);
	vsprintf(buffer, format, ap);
	va_end(ap);
	sys->message(NULL, buffer);
}
//prototype
void trace_struct_info(struct mscab_decompressor *self);

struct mscabd_cabinet* mspack_invoke_mscab_decompressor_open(struct mscab_decompressor *self, const char *filename) {	
	struct mscabd_folder_p *fol;
	struct mscabd_cabinet *cab;
	//trace_struct_info(self);
	cab = self->open(self, filename);
	fol = (struct mscabd_folder_p *) cab->files->folder;
	//trace(self, "mspack_invoke_mscab_decompressor_open: fol->data.cab->base.filename: '%s'", fol->data.cab->base.filename);
	return cab;
 }
 
 void mspack_invoke_mscab_decompressor_close(struct mscab_decompressor *self, struct mscabd_cabinet *cab) {
	self->close(self, cab);
 }

 int mspack_invoke_mscab_decompressor_extract(struct mscab_decompressor *self, struct mscabd_file *file, const char *filename) {
	struct mscabd_folder_p *fol;
	
	//trace(self, "mspack_invoke_mscab_decompressor_extract: file addr: %#x", (unsigned int)&file);
	fol = (struct mscabd_folder_p *) file->folder;//BUG: THIS folder is bad. The filename is bogus.It's fine in decompressor_open but not after comeing back from CLR!
	//trace(self, "mspack_invoke_mscab_decompressor_extract: fol->data.cab->base.filename: '%s'", fol->data.cab->base.filename);
	return self->extract(self, file, filename);
 }

int mspack_invoke_mscab_decompressor_append(struct mscab_decompressor *self, struct mscabd_cabinet *cab, struct mscabd_cabinet *nextcab) {
	return self->append(self, cab, nextcab);
}
 
 int mspack_invoke_mscab_decompressor_last_error(struct mscab_decompressor *self) {
	return self->last_error(self);
 }
 
 //Handy for replicating hte same layout of structs in C#.
 void trace_struct_info(struct mscab_decompressor *self) {
	struct mscabd_cabinet cab2;
	struct mscabd_file file;
	trace(self, "addr start: %i", (unsigned int)&cab2);
	trace(self, "addr next: %i",  (unsigned int)&cab2.next - (unsigned int)&cab2);
	trace(self, "addr filename: %i",  (unsigned int)&cab2.filename - (unsigned int)&cab2);
	trace(self, "addr base_offset: %i",  (unsigned int)&cab2.base_offset - (unsigned int)&cab2);
	trace(self, "addr length: %i",  (unsigned int)&cab2.length - (unsigned int)&cab2);
	trace(self, "addr prevcab: %i",  (unsigned int)&cab2.prevcab - (unsigned int)&cab2);
	trace(self, "addr nextcab: %i",  (unsigned int)&cab2.nextcab - (unsigned int)&cab2);
	trace(self, "addr prevname: %i",  (unsigned int)&cab2.prevname - (unsigned int)&cab2);
	trace(self, "addr nextname: %i",  (unsigned int)&cab2.nextname - (unsigned int)&cab2);
	trace(self, "addr previnfo: %i",  (unsigned int)&cab2.previnfo - (unsigned int)&cab2);
	trace(self, "addr nextinfo: %i",  (unsigned int)&cab2.nextinfo - (unsigned int)&cab2);
	trace(self, "addr files: %i",  (unsigned int)&cab2.files - (unsigned int)&cab2);
	trace(self, "addr folders: %i",  (unsigned int)&cab2.folders - (unsigned int)&cab2);
	trace(self, "addr set_id: %i",  (unsigned int)&cab2.set_id - (unsigned int)&cab2);
	trace(self, "addr set_index: %i",  (unsigned int)&cab2.set_index - (unsigned int)&cab2);
	trace(self, "addr header_resv: %i",  (unsigned int)&cab2.header_resv - (unsigned int)&cab2);
	trace(self, "addr flags: %i",  (unsigned int)&cab2.flags - (unsigned int)&cab2);
	
	trace(self, "addr start: %i", (unsigned int)&file);
	trace(self, "addr next: %i", (unsigned int)&file.next - (unsigned int)&file);
	trace(self, "addr filename: %i", (unsigned int)&file.filename - (unsigned int)&file);
	trace(self, "addr length: %i", (unsigned int)&file.length - (unsigned int)&file);
	trace(self, "addr attribs: %i", (unsigned int)&file.attribs - (unsigned int)&file);
	trace(self, "addr time_h: %i", (unsigned int)&file.time_h - (unsigned int)&file);
	trace(self, "addr time_m: %i", (unsigned int)&file.time_m - (unsigned int)&file);
	trace(self, "addr time_s: %i", (unsigned int)&file.time_s - (unsigned int)&file);
	trace(self, "addr date_d: %i", (unsigned int)&file.date_d - (unsigned int)&file);
	trace(self, "addr date_m: %i", (unsigned int)&file.date_m - (unsigned int)&file);
	trace(self, "addr date_y: %i", (unsigned int)&file.date_y - (unsigned int)&file);
	trace(self, "addr folder: %i", (unsigned int)&file.folder - (unsigned int)&file);
	trace(self, "addr offset: %i", (unsigned int)&file.offset - (unsigned int)&file);
	
	trace(self, "sizeof(off_t): %i.", sizeof(off_t));
	trace(self, "sizeof(char): %i.", sizeof(char));
	trace(self, "sizeof mscabd_cabinet:'%i'.", sizeof(struct mscabd_cabinet));
	trace(self, "sizeof mscabd_cabinet_p:'%i'.", sizeof(struct mscabd_cabinet_p));
	trace(self, "sizeof mscabd_file:'%i'.", sizeof(struct mscabd_file));
 }