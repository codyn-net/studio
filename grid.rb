require 'gtk2'
require 'allocation'
require 'components/attachment'
require 'components/gridobject'
require 'components/group'
require 'utils'
require 'groupproperties'

module Cpg
	class Grid < Gtk::DrawingArea
		type_register
	
		signal_new('activated',
			GLib::Signal::RUN_LAST,
			nil,
			nil,
			Object)
	
		signal_new('object_added',
			GLib::Signal::RUN_LAST,
			nil,
			nil,
			Object)
	
		signal_new('object_removed',
			GLib::Signal::RUN_LAST,
			nil,
			nil,
			Object)

		signal_new('popup',
			GLib::Signal::RUN_LAST,
			nil,
			nil,
			Integer,
			Bignum)
		
		signal_new('level_down',
			GLib::Signal::RUN_FIRST,
			nil,
			nil,
			Object)
	
		signal_new('level_up',
			GLib::Signal::RUN_FIRST,
			nil,
			nil,
			Object)
			
		signal_new('selection_changed',
			GLib::Signal::RUN_LAST,
			nil,
			nil)
		
		signal_new('modified',
			GLib::Signal::RUN_LAST,
			nil,
			nil)
			
		signal_new('modified_view',
			GLib::Signal::RUN_LAST,
			nil,
			nil)

		attr_reader :selection, :object_stack
		attr_accessor :grid_size
	
		def initialize
			super
		
			add_events(Gdk::Event::BUTTON1_MOTION_MASK | 
					   Gdk::Event::BUTTON3_MOTION_MASK | 
					   Gdk::Event::BUTTON_PRESS_MASK | 
					   Gdk::Event::POINTER_MOTION_MASK |
					   Gdk::Event::BUTTON_RELEASE_MASK | 
					   Gdk::Event::KEY_PRESS_MASK | 
					   Gdk::Event::KEY_RELEASE_MASK |
					   Gdk::Event::LEAVE_NOTIFY_MASK)
		
			set_can_focus(true)
		
			@max_size = 120
			@min_size = 20
			@default_grid_size = 50
			@grid_size = @default_grid_size
			@hover = []
			@focus = nil
			@signals = {}
		
			@grid_background = [1, 1, 1]
			@grid_line = [0.9, 0.9, 0.9]

			clear
		
			GLib::Timeout.add(50) do
				do_animate(50)
				true
			end
		end
		
		def signal_register(obj, signal, usewhat = :signal_connect)
			@signals[obj] ||= []
			@signals[obj] << obj.send(usewhat, signal) { |*args| yield *args }
		end
		
		def signal_unregister(obj)
			return unless @signals[obj]
			@signals[obj].each { |x| obj.signal_handler_disconnect(x) }
			
			@signals.delete(obj)
		end
		
		def signal_unregister_all
			@signals.each do |obj, signals|
				signals.each { |x| obj.signal_handler_disconnect(x) }
			end
		end
	
		def current
			@object_stack.first[:group]
		end
	
		def position
			return [current.x, current.y]
		end
		
		def grid_size=(size)
			@object_stack.first[:grid_size] = size
			@grid_size = size
			
			queue_draw
		end
	
		def root_grid_size
			return @grid_size if @object_stack.length == 1
			@object_stack[-1][:grid_size]
		end
		
		def root
			@object_stack[-1][:group]
		end
	
		def root_objects
			root.children
		end
	
		def objects
			# return objects in the current object stack container
			c = current.dup
			c.delete_if { |x| x.is_a?(Components::Attachment) }
			c
		end
		
		def each_object(objs = nil)
			(objs || current).each { |x| yield x unless x.is_a?(Components::Attachment) }
		end
	
		def attachments
			# return attachments in the current object stack container
			c = current.dup
			c.delete_if { |x| not x.is_a?(Components::Attachment) }
			c
		end
	
		def clear
			changed = @object_stack && (@object_stack.length != 1 || !@object_stack[0][:group].children.empty?)

			@object_stack = [{:group => Components::Group.new, :grid_size => @grid_size}]
			@selection = []
			@drag_state = nil

			queue_draw
			signal_emit('selection_changed')
			signal_emit('modified') if changed
		end
	
		def add_attachment(at)
			[at].flatten.each do |a|
				# find existing attachments with the same objects,
				# increase offset if found
				offsets = [-1]
				
				if a.objects.compact.length == 0
					att = attach(a.class)

					# copy properties from a to each att
					[att].flatten.each do |o|
						a.properties.each do |p, v|
							# CHECK: only copy non read only here
							o.set_property(p, v) unless (a.read_only?(p) && !(p == 'range' || p == 'initial'))
						end
					end
				else
					attachments.each do |obj|
						offsets << obj.offset if obj.same_objects(a)
					end
			
					a.offset = offsets.max + 1
					current << a
					
					object_added(a)
				end
			end
			
			signal_emit('modified')
		end
	
		def attach(klass, to = nil)
			to = @selection.dup if to == nil
			to.delete_if { |x| x.is_a?(Components::Attachment) }
			att = []
		
			while to.length >= klass.limits[0] || klass.limits[0] == -1
				break if to.empty?
			
				if klass.limits[1] == -1
					num = to.length
				else
					num = [klass.limits[1], to.length].min
				end
			
				res = klass.new(to[0..num - 1])
				
				if res.is_a?(Enumerable)
					att += res
				else
					att << res
				end
				
				add_attachment(res)
				to = to[num..-1]
			end

			signal_emit('modified')
			queue_draw
			return att
		end
	
		def <<(obj)
			add(obj)
		end
	
		def delete(obj)
			if current.include?(obj)
				current.delete(obj) 
				signal_emit('object_removed', obj)
			end
			
			unselect(obj) if @selection.include?(obj)
		
			if not obj.is_a?(Components::Attachment)
				current.delete_if do |at| 
					if at.is_a?(Components::Attachment) and at.objects.include?(obj)
						at.removed
						
						signal_emit('object_removed', at)
						true
					else
						false
					end
				end
			else
				obj.removed
			
				# reoffset other attachments with same offset
				offset = 0
			
				current.each do |at|
					if at.is_a?(Components::Attachment) and at.same_objects(obj)
						at.offset = offset
						offset += 1
					end
				end
			end
			
			signal_emit('modified')
			queue_draw
		end
		
		def object_added(obj)
			signal_emit('object_added', obj)

			obj.signal_connect('request_redraw') { |obj| queue_draw_obj(obj) }
			
			return unless obj.is_a?(Components::Group)
			
			obj.children.each do |o|
				object_added(o)
			end
		end
	
		def add(obj, x = nil, y = nil, width = nil, height = nil)
			if obj.is_a?(Class)
				if obj.ancestors.include?(Components::Attachment)
					attach(obj)
					return
				else
					obj = obj.new
				end
			end
		
			return unless obj.is_a?(Components::GridObject)
		
			if obj.is_a?(Components::Attachment)
				add_attachment(obj)
			else
				cx = ((position[0] + allocation.width / 2.0) / @grid_size.to_f).round.to_i
				cy = ((position[1] + allocation.height / 2.0) / @grid_size.to_f).round.to_i
				
				obj.allocation.x = x ? x : cx
				obj.allocation.y = y ? y : cy 
				obj.allocation.width = width ? width : 1
				obj.allocation.height = height ? height : 1
				
				ensure_unique_id(obj, obj.get_property('id'))
		
				current << obj
				object_added(obj)
				signal_emit('modified')
			end
				
			queue_draw
		end
		
		def sort_objects(objs)
			objs.select { |x| not x.is_a?(Components::Attachment) }.reverse + objs.select { |x| x.is_a?(Components::Attachment) }.reverse
		end
	
		def hittest(rect, first_only = false)
			hit = []
		
			rect.swap(0, 2) if rect[0] > rect[2]
			rect.swap(1, 3) if rect[1] > rect[3]
		
			rect[0] += current.x
			rect[1] += current.y
			rect[2] += current.x
			rect[3] += current.y
		
			rt = (0..3).collect { |i| translate(rect[i]) }
		
			sort_objects(current).each do |obj|
				if not obj.is_a?(Components::Attachment)
					a = obj.allocation
			
					hit << obj if (a.x <= rt[2] && a.x + a.width > rt[0] && a.y <= rt[3] && a.y + a.height > rt[1]) && obj.hittest(rt)
				else
					hit << obj if obj.hittest(rect, @grid_size)
				end
			
				if !hit.empty? and first_only
					return hit.first
				end
			end
		
			return nil if first_only && hit.empty?
			hit
		end
	
		def unit_size
			return translate(allocation.width, :ceil), translate(allocation.height, :ceil)
		end

		def draw_grid(ct)
			nw, nh = unit_size
		
			ct.save
			ct.set_source_rgb(*@grid_line)
			ct.scale(@grid_size, @grid_size)
			ct.line_width = 1 / @grid_size.to_f
		
			ox = (current.x / @grid_size.to_f) % 1
			oy = (current.y / @grid_size.to_f) % 1
	
			(1..nw).each do |i|
				ct.move_to(i - ox, 0)
				ct.line_to(i - ox, nh)
				ct.stroke
			end
		
			(1..nh).each do |i|
				ct.move_to(0, i - oy)
				ct.line_to(nw, i - oy)
				ct.stroke
			end
		
			ct.restore
		end
	
		def draw_object(ct, obj)
			return if obj == nil
			alloc = obj.allocation
		
			ct.save
		
			# translate to local coordinate system
			ct.translate(alloc.x, alloc.y)
		
			# scale line width according to unit size
			ct.line_width = 1 / @grid_size.to_f
			ct.font_size = ct.font_matrix.xx / @grid_size.to_f

			obj.draw(ct)
			ct.restore
		end

		def draw_objects(ct)
			# draw attachments first, then objects
			ct.save
		
			# scale to unit size
			ct.translate(-current.x, -current.y)
			ct.scale(@grid_size, @grid_size)

			items = current.sort

			items.each do |obj|
				draw_object(ct, obj) if obj.is_a?(Components::Attachment)
			end
			
			items.each do |obj|
				draw_object(ct, obj) unless obj.is_a?(Components::Attachment)
			end
		
			ct.restore
		end
	
		def draw_background(ct)
			ct.set_source_rgb(*@grid_background)
			ct.rectangle(0, 0, allocation.width, allocation.height)
			ct.fill
		end
	
		def draw_selection_rect(ct)
			return unless @mouse_rect
			ct.rectangle(@mouse_rect[0], @mouse_rect[1], @mouse_rect[2] - @mouse_rect[0], @mouse_rect[3] - @mouse_rect[1])
		
			ct.line_width = 2
			ct.set_source_rgba(0, 0, 1, 0.2)
			ct.fill_preserve
		
			ct.set_source_rgb(0, 0, 1)
			ct.stroke
		end

		def signal_do_expose_event(event)
			ct = window.create_cairo_context
			ct.rectangle(event.area.x, event.area.y, event.area.width, event.area.height)
			ct.clip
		
			draw_background(ct)
			draw_grid(ct)
			draw_objects(ct)
		
			draw_selection_rect(ct)
		
			true
		end
	
		def signal_do_configure_event(event)
			# maybe do something on resize
			true
		end
	
		def translate(pos, how = :floor)
			(pos / @grid_size.to_f).send(how).to_i
		end
	
		def translate_position(pos, how = :floor)
			(0..1).collect { |i| translate(pos[i] + position[i], how) }
		end
	
		def do_animate(ts)
			current.each do |obj|
				if obj.animate(ts)
					queue_draw_obj(obj)
				end
			end
		end
	
		def queue_draw_obj(obj)
			return if destroyed? or !current.include?(obj)

			a = obj.allocation * @grid_size
		
			# TODO also take care of attachments?
			#queue_draw_area(a.x, a.y, a.width, a.height)
			queue_draw
		end
	
		def select(obj)
			return if (obj == nil || @selection.include?(obj))

			@selection << obj		
			obj.selected = true
		
			queue_draw_obj(obj)
			signal_emit('selection_changed')
		end
	
		def unselect(obj)
			return if obj == nil or !@selection.include?(obj)

			@selection.delete(obj)
			obj.selected = false
		
			queue_draw_obj(obj)
			signal_emit('selection_changed')
		end
	
		def normalize(objs)
			# remove any links that are not fully within the group
			removed = false
			objs = objs.dup

			objs.delete_if do |obj|
				ret = false

				if obj.is_a?(Components::Attachment)
					obj.objects.each do |other|
						if not objs.include?(other)
							ret = true
							removed = true
							break
						end
					end
				end
			
				ret
			end
		
			# add any links that are encapsulated by the group
			each_object(objs.dup) do |obj|
				objs += obj.links.select do |link|
					ret = true
				
					if objs.include?(link)
						ret = false
					else
						link.objects.each do |o|
							ret = ret && objs.include?(o)
						end
					end
				
					ret
				end
			end
		
			return [removed, objs]
		end
	
		def ensure_unique_id(obj, name)
			orig = name
			sid = name
			i = 1

			ids = current.select { |o| !o.is_a?(Components::Attachment) && o != obj }.collect { |x| x.properties.include?('id') ? x.get_property('id').to_s : nil }.compact
		
			while ids.include?(sid)
				sid = "#{orig} (#{i})"
				i += 1
			end
		
			obj.set_property('id', sid)
		end
	
		def do_group_real(objs, main, klass)
			# do not group if any objects in objs that are not the main object
			# have links not included in the group
			each_object(objs) do |o|
				next if o == main
			
				o.links.each do |link|
					if not objs.include?(link)
						return false
					end
				end
			end
		
			cx, cy = mean_position(objs)
		
			g = Components::Group.new
			g.main = main
			g.klass = klass

			g.x = current.x
			g.y = current.y

			g.allocation.x = cx.floor.to_i
			g.allocation.y = cy.floor.to_i
		
			# reroute links going to main to the group
			current.each do |o|
				next unless o.is_a?(Components::Attachment)
			
				o.objects.collect! do |x|
					if x == main
						o.objects.find { |i| !objs.include?(i) } ? g : x
					else
						x
					end
				end
			end
		
			# remove objects from the grid, and add them to the group
			objs.each do |o| 
				current.delete(o)
				
				signal_emit('object_removed', o)
				unselect(o)
				g << o
			end
		
			# create id
			if g.main
				ensure_unique_id(g, g.main.to_s)
			end
		
			# add group to current
			add(g, g.allocation.x, g.allocation.y, g.allocation.width, g.allocation.height)
		
			select(g)
			
			signal_emit('modified')
			queue_draw
		end
	
		def new_group(objs)
			dlg = Gtk::Dialog.new("Group properties", 
				  self.toplevel, 
				  Gtk::Dialog::DESTROY_WITH_PARENT, 
				  [Gtk::Stock::CANCEL, Gtk::Dialog::RESPONSE_CLOSE],
				  [Gtk::Stock::OK, Gtk::Dialog::RESPONSE_OK]
			)
		
			dlg.has_separator = false		
			widget = GroupProperties.new(objs)
			dlg.vbox << widget
		
			dlg.signal_connect('response') do |d, res|
				if res == Gtk::Dialog::RESPONSE_OK
					do_group_real(objs, widget.main, widget.klass)
				end
			
				dlg.destroy
			end
		
			dlg.set_default_size(100, 50)
			dlg.show_all
		end
	
		def group(objs)
			# Create new group for objs
		
			# Check if all destinations and sources for all attachments are in
			# the group
			removed, ng = normalize(objs)
		
			return false if (removed || objs.empty?)
		
			oo = objs.select { |x| not x.is_a?(Components::Attachment) }
			return false if oo.empty?
		
			# ask for main object and render class
			new_group(objs)
			true
		end
		
		def check_link_offsets(obj)
			obj.links.each do |l1|
				others = obj.links.select { |x| x.from == l1.from && x.to == l1.to }
				
				others.each_with_index { |x, idx| x.offset = idx }
			end
		end
	
		def ungroup(obj)
			# unpack children contained in obj, reconnect links to obj to the
			# main container of obj is it has one
			return false unless obj.is_a?(Components::Group)
			return false if (!obj.links.empty? && !obj.main)
		
			# reconnect any attachments to the main object
			current.each do |link|
				next unless link.is_a?(Components::Attachment)
				
				link.objects.collect! do |x|
					if x == obj
						obj.main.link(link)
						obj.main
					else
						x
					end
				end
			end

			check_link_offsets(obj.main) if obj.main

			# make sure to set offsets correctly
		
			obj.links = []
		
			# unpack objects in group, preserving layout, center around group
			x, y = [obj.allocation.x, obj.allocation.y]
			cx, cy = mean_position(obj)

			
			# remove original object from the grid
			delete(obj)

			# change the id of main to resemble the id of the group
			pid = obj.get_property('id')
			obj.main.set_property('id', pid) if obj.main && pid && !pid.empty?
		
			obj.each do |o|
				if not o.is_a?(Components::Attachment)
					# add 0.00001 here to round 0.5 > 1
					o.allocation.x = (x + (o.allocation.x - cx) + 0.00001).round.to_i
					o.allocation.y = (y + (o.allocation.y - cy) + 0.00001).round.to_i
				end

				add(o, o.allocation.x, o.allocation.y, o.allocation.width, o.allocation.height)
				select(o)
			end

			# add any of the custom properties to the main
			if obj.main
				obj.dynamic_properties.each do |prop|
					obj.main.set_property(prop, obj.get_property(prop))
				end
			end			
			
			signal_emit('modified')						
			queue_draw
		end
	
		def mean_position(objs)
			cx = 0
			cy = 0
			n = 0
		
			each_object(objs) do |obj|
				cx += obj.allocation.x + obj.allocation.width / 2.0
				cy += obj.allocation.y + obj.allocation.height / 2.0
			
				n += 1
			end
			
			return [0, 0] if n == 0
		
			return [cx / n.to_f, cy / n.to_f]
		end
	
		def center_view
			objs = current
			cx, cy = mean_position(objs)

			cx = cx * @grid_size
			cy = cy * @grid_size
		
			# ensure cx, cy in middle of view
			current.x = cx - (allocation.width / 2.0)
			current.y = cy - (allocation.height / 2.0)
			
			signal_emit('modified_view')		
			queue_draw
		end
	
		def update_drag_state(x, y)
			if @selection.empty?
				@drag_state = nil
				return
			end
		
			# the drag state contains the relative offset of the mouse to the
			# first object in the selection. x and y are in unit coordinates
			f = nil
		
			@selection.each do |o|
				if !o.is_a?(Components::Attachment)
					f = o
					break
				end
			end
		
			if f == nil
				@drag_state = nil
				return
			end
		
			a = f.allocation
			@selection.delete(f)
			@selection.unshift(f)

			@drag_state = Allocation.new
			@drag_state.x = a.x - x
			@drag_state.y = a.y - y
		end
	
		def do_popup(button, time)
			signal_emit('popup', button, time)
		end
	
		def signal_do_button_press_event(event)
			super

			grab_focus
		
			return false if (event.button != 1 and event.button != 3 and event.button != 2)
		
			obj = hittest([event.x, event.y, event.x + 1, event.y + 1], true)
		
			if event.event_type == Gdk::Event::BUTTON2_PRESS
				return false if obj == nil
			
				signal_emit('activated', obj)
				return true
			end
		
			if event.button != 2
				if @selection.include?(obj) and (event.state.control_mask? or event.state.shift_mask?)
					unselect(obj)
					obj = nil
				end
		
				unless (event.state.control_mask? or event.state.shift_mask? or @selection.include?(obj))
					@selection.dup.each { |o| unselect(o) }
				end
			end

			if obj && event.button != 2
				select(obj)
		
				if event.button == 1
					x, y = translate_position([event.x, event.y])
					update_drag_state(x, y)
				end
			end
			
			if event.button == 3
				do_popup(event.button, event.time)
			elsif event.button == 2
				@button_press = [event.x, event.y]
				@orig_position = position
			else
				@mouse_rect = [event.x, event.y, event.x, event.y]
			end
		
			true
		end
	
		def signal_do_button_release_event(event)
			super
		
			@mouse_rect = nil
			@drag_state = nil
		
			if event.event_type == Gdk::Event::BUTTON_RELEASE
				obj = hittest([event.x, event.y, event.x + 1, event.y + 1], true)

				x, y = translate_position([event.x, event.y])
				obj.clicked(x, y) if (obj and obj.respond_to?(:clicked))
			end
		
			queue_draw
			false
		end
	
		def translate_from_drag_state(obj)
			# This translates to relative coordinates from the mouse point
			aref = @selection.first.allocation
			aobj = obj.allocation

			return @drag_state.x - (aref.x - aobj.x), @drag_state.y - (aref.y - aobj.y)
		end
	
		def do_drag_rect(event)
			@mouse_rect[2] = event.x
			@mouse_rect[3] = event.y
		
			# make sure to select only things in the rect
			objs = hittest(@mouse_rect.dup)
			
			@selection.dup.each { |o| unselect(o) unless objs.include?(o) }
			objs.each { |o| select(o) unless @selection.include?(o) }
		
			queue_draw
		end
	
		def do_move_canvas(event)
			dx = event.x - @button_press[0]
			dy = event.y - @button_press[1]
		
			current.x = @orig_position[0] - dx
			current.y = @orig_position[1] - dy
		
			signal_emit('modified_view')
			queue_draw
		end
		
		def do_mouse_in_out(event)
			obj = hittest([event.x, event.y, event.x + 1, event.y + 1], true)
			
			@hover.delete_if do |o|
				if o != obj
					o.mouse_exit
					true
				else
					false
				end
			end
			
			if obj && !@hover.include?(obj)
				obj.mouse_enter
				@hover << obj
			end
		end
	
		def signal_do_motion_notify_event(event)
			super

			if event.state.button2_mask?
				do_move_canvas(event)
				return
			end
			
			if @drag_state == nil && @mouse_rect == nil
				do_mouse_in_out(event)
				return
			end

			if @drag_state == nil
				do_drag_rect(event)
				return
			end

			# translate to unit coordinates
			x, y = translate_position([event.x, event.y])
		
			gw, gh = unit_size
			translation = []
			pxn = (current.x / @grid_size.to_f).floor
			pyn = (current.y / @grid_size.to_f).floor

			maxx = [gw - 1 + pxn]
			minx = [x]
			maxy = [gh - 1 + pyn]
			miny = [y]
				
			# determine if the move is within boundaries
			each_object(@selection) do |obj|
				xrel, yrel = translate_from_drag_state(obj)
				a = obj.allocation

				minx << -xrel + pxn
				maxx << gw - xrel + pxn - a.width
			
				miny << -yrel + pyn
				maxy << gh - yrel + pyn - a.height

				translation << [obj, xrel, yrel]
			end
		
			x = minx.max if x < minx.max
			x = maxx.min if x > maxx.min

			y = miny.max if y < miny.max
			y = maxy.min if y > maxy.min
			
			changed = false
		
			translation.each do |item|
				a = item[0].allocation
				
				if a.x != item[1] + x || a.y != item[2] + y
					a.move(item[1] + x, item[2] + y)
					changed = true
				end
			end
		
			signal_emit('modified_view') if changed
			queue_draw
		end
		
		def do_zoom(direction, x = nil, y = nil)
			nsize = @grid_size + (@grid_size * 0.2 * direction).floor.to_i
			
			if !(x && y)
				x = allocation.width / 2
				y = allocation.height / 2
			end
			
			if nsize >= @max_size
				nsize = @max_size
				upper_reached = true
			elsif nsize <= @min_size
				nsize = @min_size
				lower_reached = true
			end
			
			if upper_reached
				objs = hittest([x, y, x + 1, y + 1], false)
				objs.each do |obj|
					if obj.is_a?(Components::Group)
						# center view of the group as a policy
						cx, cy = mean_position(obj)
						
						# translate cx, cy to the mouse position 
						obj.x = cx * @default_grid_size.to_f - x
						obj.y = cy * @default_grid_size.to_f - y
						
						level_down(obj)
						return
					end
				end
			end
		
			if lower_reached && @object_stack.length > 1
				level_up(@object_stack[1][:group])
				return
			end
			
		
			current.x += ((x + current.x) * nsize.to_f / @grid_size.to_f) - (x + current.x)
			current.y += ((y + current.y) * nsize.to_f / @grid_size.to_f) - (y + current.y)
		
			changed = (nsize != @gridsize)
			@grid_size = nsize

			signal_emit('modified_view') if changed
			queue_draw
		end
	
		def signal_do_scroll_event(event)
			super

			if event.direction == Gdk::EventScroll::UP
				do_zoom(1, event.x, event.y)
				true
			elsif event.direction == Gdk::EventScroll::DOWN
				do_zoom(-1, event.x, event.y)
				true
			else
				false
			end
		end
		
		def signal_do_leave_notify_event(event)
			@hover.delete_if do |o|
				o.mouse_exit
				true
			end				
		end
	
		def signal_do_activated(obj)
			# do nothing
		end
	
		def signal_do_object_added(obj)
			signal_unregister(obj)
			
			['property_changed', 'property_added', 'property_removed'].each do |name|
				signal_register(obj, name, :signal_connect_after) { |*args| signal_emit('modified') }
			end

			['initial_changed', 'range_changed'].each do |name|
				signal_register(obj, name, :signal_connect_after) { |*args| signal_emit('modified') }
			end
		end
	
		def signal_do_object_removed(obj)
			signal_unregister(obj)
		end
		
		def do_move(dx, dy, move_canvas = false)
			return if dx == 0 && dy == 0
			
			if move_canvas
				current.x += dx * @grid_size
				current.y += dy * @grid_size
				
				signal_emit('modified_view')
			else
				each_object(@selection) do |x|
					x.allocation.x += dx
					x.allocation.y += dy
				end
				
				signal_emit('modified') unless @selection.empty?
			end
			
			queue_draw
		end
	
		def signal_do_key_press_event(ev)
			super
			
			if ev.keyval == Gdk::Keyval::GDK_Delete
				delete_objects
				true
			elsif ev.keyval == Gdk::Keyval::GDK_Home || ev.keyval == Gdk::Keyval::GDK_KP_Home
				center_view
				true
			elsif ev.keyval == Gdk::Keyval::GDK_Up || ev.keyval == Gdk::Keyval::GDK_KP_Up
				do_move(0, -1, ev.state.mod1_mask?)
				true
			elsif ev.keyval == Gdk::Keyval::GDK_Down || ev.keyval == Gdk::Keyval::GDK_KP_Down
				do_move(0, 1, ev.state.mod1_mask?)
				true
			elsif ev.keyval == Gdk::Keyval::GDK_Left || ev.keyval == Gdk::Keyval::GDK_KP_Left
				do_move(-1, 0, ev.state.mod1_mask?)
				true
			elsif ev.keyval == Gdk::Keyval::GDK_Right || ev.keyval == Gdk::Keyval::GDK_KP_Right
				do_move(1, 0, ev.state.mod1_mask?)
				true
			elsif ev.keyval == Gdk::Keyval::GDK_plus || ev.keyval == Gdk::Keyval::GDK_KP_Add
				do_zoom(1)
				true
			elsif ev.keyval == Gdk::Keyval::GDK_minus || ev.keyval == Gdk::Keyval::GDK_KP_Subtract
				do_zoom(-1)
				true
			elsif ev.keyval == Gdk::Keyval::GDK_Tab || ev.keyval == Gdk::Keyval::GDK_ISO_Left_Tab
				focus_next(ev.state.shift_mask? ? -1 : 1)
			elsif ev.keyval == Gdk::Keyval::GDK_space
				o = @focus
				
				if @focus && !@focus.selected
					select(@focus)
					focus_set(o)
				elsif @focus && @focus.selected
					unselect(@focus)
					focus_set(o)
				end
			elsif ev.keyval == Gdk::Keyval::GDK_Menu || ev.keyval == Gdk::Keyval::GDK_Multi_key
				do_popup(3, ev.time)
			elsif ev.keyval == Gdk::Keyval::GDK_Return || ev.keyval == Gdk::Keyval::GDK_KP_Enter
				signal_emit('activated', @selection.first) if @selection.length == 1
			else
				false
			end
		end
		
		def focus_next(direction)
			pf = @focus
			focus_release
			
			if !pf
				@focus = current[direction == 1 ? 0 : -1]
			else
				nidx = current.index(pf) + direction
				
				return false if nidx >= current.length || nidx < 0
				@focus = current[nidx]
			end
			
			if @focus
				focus_set(@focus)
				true
			else
				false
			end
		end
		
		def focus_set(o)
			@focus = o
			@focus.focus = true
			queue_draw_obj(@focus)
		end
		
		def focus_release
			if @focus
				o = @focus
				@focus = nil
				
				o.focus = false
				queue_draw_obj(o)
			end
		end
	
		def signal_do_popup(button, time)
		end
	
		def delete_objects
			@selection.dup.each do |obj|
				delete(obj)
			end
		end
	
		def level_down(obj)
			return false unless current.include?(obj)
		
			signal_emit('level_down', obj)
			true
		end
	
		def level_up(obj)
			while @object_stack.length > 1 && current != obj
				signal_emit('level_up', current)
			end

			true
		end
	
		def signal_do_level_down(obj)
			@object_stack.unshift({:group => obj, :grid_size => @grid_size})
			@grid_size = @default_grid_size

			signal_emit('modified_view')
			queue_draw
		end
	
		def signal_do_level_up(obj)
			a = @object_stack.shift
			@grid_size = a[:grid_size]

			signal_emit('modified_view')
			queue_draw
		end
		
		def signal_do_selection_changed
			focus_release
		end
		
		def signal_do_focus_out_event(ev)
			@hover.delete_if do |x|
				x.mouse_exit
				true
			end
			
			focus_release
		end
		
		def signal_do_modified
		end
		
		def signal_do_modified_view
		end
	end
end
