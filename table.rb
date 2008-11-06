require 'gtk2'
require 'signalregistry'

module Cpg
	class Table < Gtk::Table
		type_register
	
		include SignalRegistry
	
		attr_accessor :expand

		EXPAND_RIGHT = 1
		EXPAND_DOWN = 2
	
		class Placeholder < Gtk::DrawingArea
			type_register
		
			def initialize(*args)
				super
			
				show
			end
		end
	
		def initialize(rows = 1, cols = 1, homogeneous = false)
			super({'n-rows' => rows, 'n-columns' => cols, 'homogeneous' => homogeneous})
		
			@expand = EXPAND_RIGHT
			@signals = {}
		
			add_events(Gdk::Event::KEY_PRESS_MASK | Gdk::Event::BUTTON_PRESS_MASK)
			set_can_focus(true)

			Gtk::Drag.dest_set(self, 0, [['TableItem', Gtk::Drag::TARGET_SAME_APP, 1]], Gdk::DragContext::ACTION_MOVE)
		end
	
		def child_position(child, left, top)
			child_set_property(child, 'left_attach', left)
			child_set_property(child, 'right_attach', left + 1)
			child_set_property(child, 'top_attach', top)
			child_set_property(child, 'bottom_attach', top + 1)
		end
	
		def move_to(from, to)
			['left', 'right', 'top', 'bottom'].each do |name|
				child_set_property(from, "#{name}_attach", child_get_property(to, "#{name}_attach"))
			end
		end
	
		def do_update_dragging(x, y)
			# we have the child in @dragging which was dragged from @dragpos, we
			# need to determine the child position under the cursor at x, y
			# then swap accordingly
		
			mtx = Array.new(self.n_columns)
			mtx.collect! { |idx| Array.new(self.n_rows) }
		
			columns = Array.new(self.n_columns)
			rows = Array.new(self.n_rows)
		
			children.each do |child|
				a = child.allocation
				left = child_get_property(child, 'left_attach')
				top = child_get_property(child, 'top_attach')
			
				columns[left] = [a.x, a.x + a.width]
				rows[top] = [a.y, a.y + a.height]
			
				mtx[left][top] = child
			end

			col = nil
			columns.each_with_index do |ptr, idx|
				if ptr[0] < x && ptr[1] > x
					col = idx 
					break
				end
			end
		
			row = nil
			rows.each_with_index do |ptr, idx|
				if ptr[0] < y && ptr[1] > y
					row = idx
					break
				end
			end
		
			return unless (row && col)
		
			found = mtx[col][row]
			return if found == @dragging
		
			if @swapped
				# first make sure to move current swapped back to position drag
				# is now at
				move_to(@swapped, @dragging)
			end
		
			if @swapped && @swapped == found
				found = @dragging
				@swapped = nil
			else
				# now move @dragging to new position
				child_position(@dragging, col, row)
		
				# set swapped to newly found
				@swapped = found
			end
		
			child_position(found, @dragpos[0], @dragpos[1]) if found
		end
	
		def find_empty
			all = []
		
			(0..self.n_columns - 1).each do |x|
				(0..self.n_rows - 1).each do |y|
					all << [x, y]
				end
			end
		
			if @expand == EXPAND_DOWN
				all.sort! { |a, b| a[1] == b[1] ? a[0] <=> b[0] : a[1] <=> b[1] }
			end

			children.each do |child|
				left = child_get_property(child, 'left_attach')
				top = child_get_property(child, 'top_attach')
			
				all.delete_if { |x| x == [left, top] }
			end	
		
			return all[0] unless all.empty?
		
			# otherwise we need to resize
			if @expand == EXPAND_RIGHT
				resize(self.n_rows, self.n_columns + 1)
				return [self.n_columns - 1, 0]
			else
				resize(self.n_rows + 1, self.n_columns)
				return [0, self.n_rows - 1]
			end
		end
	
		def <<(child)
			add(child)
		end
	
		def add(child)
			left, top = find_empty
		
			if (child.flags & Gtk::Widget::NO_WINDOW)
				ev = Gtk::EventBox.new
				child.show
			
				child.signal_connect('destroy') { |widget| ev.destroy }
				ev << child
				child = ev
			end
		
			attach(child, left, left + 1, top, top + 1, Gtk::EXPAND | Gtk::FILL, Gtk::EXPAND | Gtk::FILL, 0, 0)
		
			set_drag_source(child)
			child.show
		end

		def set_drag_source(child)
			Gtk::Drag.source_set(child, Gdk::Window::ModifierType::BUTTON1_MASK, [['TableItem', Gtk::Drag::TARGET_SAME_APP, 1]], Gdk::DragContext::ACTION_MOVE)

			connect(child, 'drag-begin') { |c, ctx| do_drag_begin(c, ctx) }
			connect(child, 'drag-end') { |c, ctx| do_drag_end(c, ctx) }
		end
	
		def do_drag_begin(child, context)
			# create drag icon
			alloc = child.allocation
			pix = Gdk::Pixbuf.from_drawable(nil, child.window, 0, 0, alloc.width, alloc.height)

			Gtk::Drag.set_icon(context, pix, alloc.width / 2, alloc.height / 2)
			@dragging = child
			@swapped = nil
		
			@dragpos = [child_get_property(child, 'left-attach'),
						child_get_property(child, 'top-attach')]
		end
	
		def do_drag_end(child, context)
			@dragging = nil
			
			compact
		end
		
		def empty_row?(row)
			!children.find { |child| !child.is_a?(Placeholder) && child_get_property(child, 'top-attach') == row }
		end
		
		def empty_column?(col)
			!children.find { |child| !child.is_a?(Placeholder) && child_get_property(child, 'left-attach') == col }
		end
		
		def find_empty_row
			return nil if self.n_rows <= 1
			(0..self.n_rows - 1).find { |x| empty_row?(x) }
		end
		
		def find_empty_column
			return nil if self.n_columns <= 1
			(0..self.n_columns - 1).find { |x| empty_column?(x) }
		end
		
		def remove_column(col)
			# move all children with left-attach > col to -1
			children.each do |child| 
				left = child_get_property(child, 'left-attach')
				
				if left == col && child.is_a?(Placeholder)
					child.destroy
				else
					child_set_property(child, 'left-attach', left - 1) if left && left > col
				end
			end
			
			resize(self.n_rows, self.n_columns - 1)
			
			queue_draw
		end
		
		def remove_row(row)
			# move all children with top-attach > row to -1
			children.each do |child| 
				top = child_get_property(child, 'top-attach')
				
				if top == row && child.is_a?(Placeholder)
					child.destroy
				else
					child_set_property(child, 'top-attach', top - 1) if top && top > row
				end
			end
			
			resize(self.n_rows - 1, self.n_columns)
			
			queue_draw
		end

		def compact
			return if destroyed?

			# remove any empty rows or columns
			while ((r = find_empty_row))
				remove_row(r)
			end
			
			while ((c = find_empty_column))
				remove_column(c)
			end
		end
		
		def signal_do_destroy
			@destroying = true
			super
		end
	
		def signal_do_remove(w)
			super
			
			return if @destroying
			
			(@signals[w] || []).each { |s| w.signal_handler_disconnect(s) }
			@signals.delete(w)
			
			compact unless w.is_a?(Placeholder)
		end
	
		def find_empty_row_col(last, what)
			placeholder = nil
			ret = children.find do |child| 
				if child_get_property(child, "#{what}-attach") == last
					r = !child.is_a?(Placeholder)
					placeholder = child unless r
				
					r
				else
					false
				end
			end
		
			return [ret, placeholder]
		end
	
		def remove_empty_row
			last = self.n_rows - 1
			ret, placeholder = find_empty_row_col(last, 'top')
		
			return false if ret
		
			placeholder.destroy if placeholder
			remove_row(last)

			true
		end
	
		def remove_empty_column
			last = self.n_columns - 1
			ret, placeholder = find_empty_row_col(last, 'left')
		
			return false if ret
		
			placeholder.destroy if placeholder
			remove_column(last)

			true
		end
	
		def signal_do_key_press_event(ev)
			return false unless ev.state.control_mask?

			case ev.keyval
				when Gdk::Keyval::GDK_Down
					return remove_empty_row
				when Gdk::Keyval::GDK_Up
					resize(self.n_rows + 1, self.n_columns)
					attach(Placeholder.new, 0, 1, self.n_rows - 1, self.n_rows)
					queue_draw
					return true
				when Gdk::Keyval::GDK_Left
					resize(self.n_rows, self.n_columns + 1)
					attach(Placeholder.new, self.n_columns - 1, self.n_columns, 0, 1)
					queue_draw
					return true
				when Gdk::Keyval::GDK_Right
					return remove_empty_column
			end
		
			false
		end
	
		def signal_do_drag_motion(ctx, x, y, time)
			super
		
			if !@dragging
				ctx.drag_status(0, time)
				false
			else
				do_update_dragging(x, y)
				ctx.drag_status(Gdk::DragContext::ACTION_MOVE, time)
				true
			end
		end
		
		def signal_do_button_press_event(ev)
			grab_focus
			false
		end
		
		def signal_do_drag_drop(ctx, x, y, time)
			return false unless @dragging
			
			Gtk::Drag.finish(ctx, true, false, time)
		end
	end
end
