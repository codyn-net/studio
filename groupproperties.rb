require 'gtk2'
require 'components/group'

module Cpg
	class GroupProperties < Gtk::Table
		type_register
		
		attr_reader :combo_main, :combo_class
	
		def initialize(objs, defmain = nil, defklass = nil)
			super({
					'n_rows' => 2, 
					'n_columns' => 2, 
					'homogeneous' => false, 
					'row_spacing' => 3, 
					'column_spacing' => 3
			})
		
			@objs = objs.select { |x| not x.is_a?(Components::Attachment) }
			build(defmain, defklass)
			show_all
		end
	
		def main
			@combo_main.active_iter ? @combo_main.active_iter[0] : nil
		end
	
		def klass
			@combo_class.active_text
		end
		
		def make_label(lbl)
			l = Gtk::Label.new(lbl)
			l.xalign = 0
			
			l
		end
	
		def build(defmain, defklass)
			attach(make_label('Relay:'), 0, 1, 0, 1, Gtk::FILL, Gtk::FILL)
			attach(make_label('Class:'), 0, 1, 1, 2, Gtk::FILL, Gtk::FILL)
		
			store = Gtk::ListStore.new(Object)
			activeidx = 0
			idx = 1
			
			piter = store.append
			piter[0] = nil
			
			@objs.each do |obj|
				next unless obj.property_set?('id')

				piter = store.append
				piter[0] = obj
				
				if obj == defmain
					activeidx = idx
				end
				
				idx = idx + 1
			end
		
			combo = Gtk::ComboBox.new(store)
			renderer = Gtk::CellRendererText.new
			combo.pack_start(renderer, true)
			combo.set_cell_data_func(renderer) do |layout, cell, model, piter|
				cell.text = piter[0] ? piter[0].to_s : 'None'
			end
			combo.active = activeidx
			@combo_main = combo
		
			attach(combo, 1, 2, 0, 1)
			
			activeidx = 0
			idx = 0
			
			combo = Gtk::ComboBox.new
			Components::Groups::Renderer.classes.each do |klass|
				cname = klass.to_s.gsub(/.*::/, '')
				combo.append_text(cname)
				
				if cname == defklass.to_s
					activeidx = idx
				end
				
				idx = idx + 1
			end
			
			combo.active = activeidx
			@combo_class = combo
		
			attach(combo, 1, 2, 1, 2)
		end
	end
end
