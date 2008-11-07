require 'graph'
require 'stock'
require 'simulation'
require 'hook'
require 'table'

module Cpg
	class Monitor < Hook
		type_register

		SAMPLE_WIDTH = 2
	
		def initialize(dt)
			super({:title => 'Monitor'})
		
			@dt = dt
			@coloridx = 0
			@linkrulers = true
			@linkaxis = true

			build
			
			signal_register(Simulation.instance, 'step') { |s| update(s.timestep) }
			signal_register(Simulation.instance, 'period_start') { |s, from, timestep, to| do_period_start(from, timestep, to) }
			signal_register(Simulation.instance, 'period_stop') { |s| do_period_stop }
		
			set_default_size(500, 400)
			
			ag = Gtk::ActionGroup.new('MonitorActions')
			
			ag.add_actions([
				['AutoAxisAction', nil, 'Auto scale axis', '<Control>r', 'Automatically scale axis for data', Proc.new { |g, a| do_auto_axis }]
			])
			
			ag.add_toggle_actions([
				['LinkedAxisAction', nil, 'Link axis scales', '<Control>l', 'Link graph axis scales', Proc.new { |g, a| do_link_axis(a.active?) }, @linkaxis]
			])
			
			@uimanager.insert_action_group(ag, 0)
			
			mid = @uimanager.new_merge_id
			@uimanager.add_ui(mid, "/menubar/View/ViewBottom", "AutoAxis", "AutoAxisAction", Gtk::UIManager::MENUITEM, false)
			@uimanager.add_ui(mid, "/menubar/View/ViewBottom", "LinkedAxis", "LinkedAxisAction", Gtk::UIManager::MENUITEM, false)  
		end
		
		def sort_hook(a, b)
			aw = @content.child_real(a[1][:widget])
			bw = @content.child_real(b[1][:widget])
			
			return 0 if !aw || !bw
			
			al = @content.child_get_property(aw, 'left-attach')
			at = @content.child_get_property(aw, 'top-attach')
			
			bl = @content.child_get_property(bw, 'left-attach')
			bt = @content.child_get_property(bw, 'top-attach')
			
			if @content.expand == Table::EXPAND_DOWN
				at == bt ? al <=> bl : at <=> bt
			else
				al == bl ? at <=> bt : al <=> bl
			end
		end
		
		def set_size(cols, rows)
			cols = cols.to_i
			rows = rows.to_i
			
			cols = @content.n_columns if cols <= 0
			rows = @content.n_rows if rows <= 0
			
			@content.resize(rows, cols)
		end
		
		def size
			return [@content.n_columns, @content.n_rows]
		end
		
#		def under_cursor(x, y)
#			@content.children.find do |child|
#				alloc = child.allocation
#				
#				if x > alloc.x && x < alloc.x + alloc.width &&
#				   y > alloc.y && y < alloc.y + alloc.height
#					true
#				else
#					false
#				end
#			end
#		end
		
		def content_area
			@content = Table.new(1, 1, true)
			@content.expand = Table::EXPAND_DOWN
			@content.row_spacings = 1
			@content.column_spacings = 1
			
#			Gtk::Drag.dest_set(@content, 0, [['CpgGraph', Gtk::Drag::TARGET_SAME_APP, 1]], Gdk::DragContext::ACTION_MOVE)
			
#			@content.signal_connect('drag-motion') do |c, ctx, x, y, time|
#				if @dragging
#					other = under_cursor(x, y)
#					
#					if other && other != @dragging
#						@content.reorder_child(@dragging, @content.child_get_property(other, 'position'))
#					end

#					ctx.drag_status(Gdk::DragContext::ACTION_MOVE, time)
#					true
#				else
#					false
#				end
#			end
			
			@content
		end
		
		def do_link_axis(active)
			@linkaxis = active
			
			if @linkaxis
				yax = [nil, nil]
				
				each_graph do |obj, container|
					y = container[:graph].yaxis
					
					yax[0] = y[0] if (yax[0] == nil || y[0] < yax[0])
					yax[1] = y[1] if (yax[1] == nil || y[1] > yax[1])
				end
				
				return unless yax[0] && yax[1]
				
				each_graph do |obj, container|
					container[:graph].yaxis = yax
				end
			end
		end
		
		def do_auto_axis
			each_graph do |obj, container|
				container[:graph].auto_axis
			end
			
			do_link_axis(@linkaxis)
		end
		
		def build
			hbox = Gtk::HBox.new(false, 6)
			
			but = Stock.chain_button do |x| 
				@linkrulers = x.active?
			end
			
			but.active = @linkrulers
			
			hbox.pack_end(but, false, false, 0)

			@showrulers = Gtk::CheckButton.new('Show graph rulers')
			@showrulers.active = true
			hbox.pack_end(@showrulers, false, false, 0)
						
			@vbox_content.pack_end(hbox, false, false, 3)
			
			@showrulers.signal_connect('toggled') do |t|
				each_graph do |o, x|
					x[:graph].show_ruler = t.active?
				end
			end
			
			hbox.show_all
		end
		
		def install_object(obj, prop)
			super
			
			signal_register(obj, 'property_changed') do |o, p| 
				if ['display', 'label', 'equation', 'id'].include?(p)
					update_title(o, prop)
				end
			end
		end
		
		def remove_hook_real(obj, container)
			g = container[:graph]
			
			g.signal_handler_disconnect(container[:configure]) if container[:configure]

			if g.num_plots > 1
				# prevent removal of graph
				g.remove(container[:plot])
				container[:widget] = nil
			end
			
			super
			
			Simulation.instance.unset_monitor(obj, container[:prop])
		end
		
		def set_yaxis(obj, prop, yax)
			@map[obj].each do |p|
				if p[:prop] == prop
					p[:graph].yaxis = yax
					return
				end
			end
		end
		
		def yaxis(obj, prop)
			@map[obj].each do |p|
				if p[:prop] == prop
					return p[:graph].yaxis
				end
			end
			
			return [-3, 3]
		end
	
		def add_hook_real(obj, prop, state)
			@map[obj].each do |p|
				return if p[:prop] == prop
			end

			g = Graph.new(SAMPLE_WIDTH, [-3, 3])
			g.set_size_request(-1, 50)
			g.show_ruler = @showrulers.active?
			
			if @linkaxis && !@map.empty? && !@map[@map.keys[0]].empty?
				g.yaxis = @map[@map.keys[0]][0][:graph].yaxis
			end
			
			hbox = Gtk::HBox.new(false, 1)
			#exp = Gtk::Expander.new(property_name(obj, prop, true))
		#	vs = Gtk::VBox.new(false, 3)
			#vs.pack_start(g, true, true, 0)
			
			frame = Gtk::Frame.new
			frame.shadow_type = Gtk::ShadowType::ETCHED_IN
			frame << g
			
			# FIXME: no scrollbars for now
			#vs.pack_start(Gtk::HScrollbar.new(g.adjustment), false, false, 0)
		
			#exp.add(vs)
			#exp.expanded = true
		
			#hbox.pack_start(exp, true, true, 0)
			hbox.pack_start(frame, true, true, 0)
			
			close = Stock.close_button { |x| remove_all_hooks(obj, prop) }
		
			align = Gtk::VBox.new(false, 3)
			align.pack_start(close, false, false, 0)
			
			merge = Stock.small_button(Gtk::Stock::GOTO_TOP) { |x| do_merge(obj, prop, 0, -1) }
			align.pack_start(merge, false, false, 0)
			
			merge = Stock.small_button(Gtk::Stock::GOTO_BOTTOM) { |x| do_merge(obj, prop, 0, 1) }
			align.pack_start(merge, false, false, 0)
			
			merge = Stock.small_button(Gtk::Stock::GOTO_LAST) { |x| do_merge(obj, prop, 1, 0) }
			align.pack_start(merge, false, false, 0)
			
			merge = Stock.small_button(Gtk::Stock::GOTO_FIRST) { |x| do_merge(obj, prop, -1, 0) }
			align.pack_start(merge, false, false, 0)
		
			hbox.pack_start(align, false, false, 0)
			hbox.show_all

#			Gtk::Drag.source_set(g, Gdk::Window::ModifierType::BUTTON1_MASK, [['CpgGraph', Gtk::Drag::TARGET_SAME_APP, 1]], Gdk::DragContext::ACTION_MOVE)
#			
#			g.signal_connect('drag-begin') do |gr, ctx|
#				pix = Gdk::Pixbuf.from_drawable(nil, g.window, 0, 0, g.allocation.width, g.allocation.height)

#				Gtk::Drag.set_icon(ctx, pix, g.allocation.width / 2, g.allocation.height / 2)
#				
#				@dragging = hbox
#			end
#			
#			g.signal_connect('drag-end') do |gr, ctx|
#				@dragging = nil
#			end
			
			#@content.pack_start(hbox, true, true, 0)
			@content << hbox
		
			#exp.signal_connect('activate') do |o|
			#	active = !exp.expanded?
			
			#	@content.set_child_packing(hbox, active, active, 0, Gtk::PackType::START)
			#end
			
			g.signal_connect_after('motion_notify_event') do |o,ev|
				do_linkrulers(g, ev) if @linkrulers && g.show_ruler
			end
			
			g.signal_connect_after('leave_notify_event') do |o,ev|
				do_linkrulers_leave(g, ev) if @linkrulers && g.show_ruler
			end
			
			# register monitoring
			Simulation.instance.set_monitor(obj, prop)

			container = super(obj, prop, state.merge({:graph => g, :widget => hbox, :plot => (g << [])})) #, :expander => exp}))
			
			container[:plot].label = property_name(obj, prop, true)
			
			# request resimulate
			Simulation.instance.resimulate
		end
		
		def remove_all_hooks(obj, prop)
			h = find_hook(obj, prop)
			
			return unless h
			widget = h[:widget]
			
			items = find_for_widget(widget)
			
			items.each do |x|
				remove_hook(x[0], x[1][:prop])
			end
		end
		
		def find_for_widget(w)
			res = []
			
			@map.each do |obj, lst|
				lst.each { |x| res << [obj, x] if x[:widget] == w }
			end
			
			res
		end
		
		def do_merge(obj, prop, dx, dy)
			h = find_hook(obj, prop)
			
			return unless h
			
			widget = h[:widget]		
			to = @content.find(widget, dx, dy)
			
			return unless to
			
			to = to.children.first
			
			items = find_for_widget(widget)
			toitem = find_for_widget(to).first[1]
			
			# merge items with the found graph
			items.each do |item|
				o = item[0]
				item = item[1]
				
				item[:plot] = toitem[:graph].add(item[:plot].data, item[:plot].color)
				item[:graph] = toitem[:graph]
				item[:widget] = to
				
				GLib::Source.remove(item[:configure]) if item[:configure]
				item[:configure] = nil
			end
			
			update_title(obj, prop)
			
			widget.destroy
		end
		
		def do_linkrulers(g, ev)
			# propagate ruler position to other graphs
			each_graph do |obj, x|
				x[:graph].ruler = g.ruler if x[:graph] != g
			end
			
			false
		end
		
		def do_linkrulers_leave(g, ev)
			# remove rulers
			each_graph do |obj, x|
				x[:graph].ruler = nil
			end
			
			false
		end
		
		def update_title(obj, prop)
			h = find_hook(obj, prop)
			
			return unless h
			
			h[:plot].label = property_name(obj, prop, true)
		end
		
		def do_period_start(from, timestep, to)
			@inperiod = true
			@range = [from, timestep, to]
		end
		
		def each_graph
			@map.each do |obj, p|
				p.each do |v|
					yield obj, v
				end
			end
		end
		
		def set_monitor_data(obj, v)
			numpix = v[:graph].allocation.width
				
			# resample data to be on pxd
			rstep = (@range[2] - @range[0]) / numpix.to_f;
			
			to = (0...(numpix / SAMPLE_WIDTH)).collect { |x| @range[0] + (x * rstep * SAMPLE_WIDTH) }
			
			data = Simulation.instance.monitor_data_resampled(obj, v[:prop], to)
			data.collect! { |x| (x.to_f.nan? || x.to_f.infinite?) ? 0.0 : x.to_f } 

			v[:plot].data = data || []
		end
		
		def update_monitor_data(obj, v)
			# find all plots
			items = find_for_widget(v[:widget])
			
			items.each do |x|
				set_monitor_data(x[0], x[1])
			end
		end
		
		def set_configure_handler(obj, v)
			if !v[:configure]				
				v[:configure] = v[:graph].signal_connect('configure-event') do |g, ev|
					GLib::Source.remove(v[:configure_source]) if v[:configure_source]
					
					v[:configure_source] = GLib::Timeout.add(50) do
						v[:configure_source] = nil
						update_monitor_data(obj, v)
						false
					end
				end
			end
		end
		
		def do_period_stop
			# configure graphs so that they show the collected data correctly
			configures = {}
			
			each_graph do |obj, v|
				set_monitor_data(obj, v)
				
				g = v[:graph]
				set_configure_handler(obj, v) unless configures[g]
				configures[g] = true
			end
		end
		
		def reset_graphs
			each_graph do |obj, v|
				v[:plot].data = []
				v[:graph].yaxis = [-3, 3]
			end
		end
	
		def update(timestep)
#			if @inperiod && !Simulation.instance.period?
#				reset_graphs
#				@inperiod = false
#			end
#			
#			return if Simulation.instance.period?

#			@map.each do |obj, p|
#				c = Simulation.instance.setup_context(obj)

#				p.each do |v|
#					GLib::Source.remove(v[:configure_source]) if v[:configure_source]
#					v[:graph].signal_handler_disconnect(v[:configure])
#					v.delete(:configure)
#					v.delete(:configure_source)
#					
#					v[:graph] << Simulation.instance.monitor_data(obj, v[:prop])[-1]
#				end
#			end
		end
	end
end
